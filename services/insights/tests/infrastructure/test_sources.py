from datetime import timedelta
from pathlib import Path
from uuid import uuid4

import httpx
import pytest

from incident_insights.application.ports import IncidentSourceError
from incident_insights.domain.incidents.timeline import TimelineKind
from incident_insights.domain.shared.reporting_window import ReportingWindow
from incident_insights.infrastructure.incidents.acl import IncidentTranslator
from incident_insights.infrastructure.incidents.api_source import (
    API_KEY_HEADER,
    ApiIncidentSource,
    create_api_client,
)
from incident_insights.infrastructure.incidents.cache import CachedIncidentSource
from incident_insights.infrastructure.incidents.csv_source import CsvIncidentSource
from incident_insights.infrastructure.incidents.factory import build_api_client, build_source
from incident_insights.infrastructure.incidents.retry import RetryingGetter, RetryPolicy
from incident_insights.infrastructure.settings import DataSourceKind, Settings
from tests.builders import AS_OF, make_record, payload_json, window_of
from tests.fakes import InMemoryIncidentSource

WINDOW = window_of(30)
TRANSLATOR = IncidentTranslator()


def _api(handler: httpx.MockTransport) -> ApiIncidentSource:
    client = httpx.Client(base_url="https://incidents.test", transport=handler)
    getter = RetryingGetter(client, RetryPolicy(), sleep=lambda _delay: None)
    return ApiIncidentSource(getter, TRANSLATOR)


def _json(payload: object, status: int = 200) -> httpx.MockTransport:
    return httpx.MockTransport(lambda _request: httpx.Response(status, json=payload))


def test_api_export_sends_window_and_translates_records() -> None:
    record = make_record()
    seen: list[httpx.Request] = []

    def handler(request: httpx.Request) -> httpx.Response:
        seen.append(request)
        return httpx.Response(200, json=[payload_json(record)])

    history = _api(httpx.MockTransport(handler)).export(WINDOW)

    assert [item.id for item in history] == [record.id]
    assert seen[0].url.path == "/api/incidents/export"
    assert seen[0].url.params["from"] == "2026-08-29T00:00:00Z"
    assert seen[0].url.params["to"] == "2026-09-28T00:00:00Z"


def test_api_get_translates_incident_with_timeline() -> None:
    record = make_record()
    entry = {
        "id": str(uuid4()),
        "incidentId": str(record.id),
        "at": "2026-09-25T00:00:00Z",
        "kind": "Note",
        "actor": "alice",
        "message": "Rolled back",
    }

    found = _api(_json({**payload_json(record), "timeline": [entry]})).get(record.id)

    assert found is not None
    assert found.timeline[0].kind is TimelineKind.NOTE


def test_api_get_returns_none_when_missing() -> None:
    assert _api(_json({"title": "Not Found"}, 404)).get(uuid4()) is None


@pytest.mark.parametrize("status", [500, 503, 401])
def test_api_errors_raise_source_error(status: int) -> None:
    with pytest.raises(IncidentSourceError, match=str(status)):
        _api(_json({}, status)).export(WINDOW)


def test_api_invalid_payload_raises_source_error() -> None:
    with pytest.raises(IncidentSourceError, match="invalid export"):
        _api(_json([{"id": "not-a-uuid"}])).export(WINDOW)


def test_api_payload_violating_domain_rules_raises_source_error() -> None:
    payload = {**payload_json(make_record()), "serviceId": "Not A Slug"}

    with pytest.raises(IncidentSourceError, match="invalid export"):
        _api(_json([payload])).export(WINDOW)


def test_api_invalid_incident_raises_source_error() -> None:
    with pytest.raises(IncidentSourceError, match="invalid incident"):
        _api(_json({"id": "x"})).get(uuid4())


def test_api_unreachable_raises_source_error() -> None:
    def handler(request: httpx.Request) -> httpx.Response:
        raise httpx.ConnectError("refused", request=request)

    with pytest.raises(IncidentSourceError, match="unreachable"):
        _api(httpx.MockTransport(handler)).export(WINDOW)


def test_csv_source_filters_window(sample_csv: Path) -> None:
    history = CsvIncidentSource(sample_csv, TRANSLATOR).export(WINDOW)

    assert not history.is_empty
    assert all(WINDOW.contains(record.created_at) for record in history)


def test_csv_source_get_derives_chronology(sample_csv: Path) -> None:
    source = CsvIncidentSource(sample_csv, TRANSLATOR)
    record = next(iter(source.export(WINDOW)))

    found = source.get(record.id)

    assert found is not None
    assert found.chronology()[0].kind is TimelineKind.TRIGGERED
    assert source.get(uuid4()) is None


def test_csv_source_missing_file_raises(tmp_path: Path) -> None:
    with pytest.raises(IncidentSourceError, match="not found"):
        CsvIncidentSource(tmp_path / "missing.csv", TRANSLATOR).export(WINDOW)


def test_csv_source_invalid_rows_raise(tmp_path: Path) -> None:
    path = tmp_path / "broken.csv"
    path.write_text("id,number\nnot-a-uuid,1\n", encoding="utf-8")

    with pytest.raises(IncidentSourceError, match="invalid"):
        CsvIncidentSource(path, TRANSLATOR).export(WINDOW)


def test_cache_reuses_hour_aligned_export_until_ttl() -> None:
    now = [0.0]
    inner = InMemoryIncidentSource([make_record(created_at=AS_OF - timedelta(minutes=10))])
    cached = CachedIncidentSource(inner, ttl_seconds=60, clock=lambda: now[0])
    end = AS_OF + timedelta(minutes=5)
    start = WINDOW.start

    first = cached.export(ReportingWindow(start + timedelta(minutes=7), end))
    second = cached.export(
        ReportingWindow(start + timedelta(minutes=8), end + timedelta(minutes=1))
    )
    now[0] = 61.0
    cached.export(ReportingWindow(start + timedelta(minutes=8), end))

    assert first == second
    assert len(inner.export_calls) == 2
    assert inner.export_calls[0] == ReportingWindow(start, AS_OF + timedelta(hours=1))


def test_cache_filters_to_requested_window() -> None:
    inside = make_record(number=1, created_at=AS_OF - timedelta(minutes=30))
    outside = make_record(number=2, created_at=AS_OF - timedelta(minutes=50))
    cached = CachedIncidentSource(InMemoryIncidentSource([inside, outside]), ttl_seconds=60)

    history = cached.export(ReportingWindow(AS_OF - timedelta(minutes=40), AS_OF))

    assert list(history) == [inside]


def test_cache_passes_lookups_through() -> None:
    record = make_record()
    cached = CachedIncidentSource(InMemoryIncidentSource([record]), ttl_seconds=60)

    assert cached.get(record.id) is record


def test_factory_builds_csv_or_cached_api_source(sample_csv: Path) -> None:
    csv_settings = Settings(insights_data_source=DataSourceKind.CSV, insights_csv_path=sample_csv)

    assert isinstance(build_source(csv_settings), CsvIncidentSource)
    assert isinstance(build_source(Settings()), CachedIncidentSource)


def test_api_client_sends_the_api_key_on_every_request() -> None:
    client = create_api_client("https://incidents.test", 5.0, "service-key")

    request = client.build_request("GET", "/api/incidents/export")

    assert request.headers[API_KEY_HEADER] == "service-key"
    assert request.headers["Accept"] == "application/json"


def test_api_client_omits_the_api_key_when_not_configured() -> None:
    client = create_api_client("https://incidents.test", 5.0, "")

    assert API_KEY_HEADER not in client.headers


def test_factory_client_reads_the_api_key_from_settings() -> None:
    configured = build_api_client(
        Settings(incidents_api_base_url="https://api.test", incidents_api_key="service-key")
    )

    assert configured.headers[API_KEY_HEADER] == "service-key"
    assert str(configured.base_url) == "https://api.test"
    assert API_KEY_HEADER not in build_api_client(Settings()).headers
