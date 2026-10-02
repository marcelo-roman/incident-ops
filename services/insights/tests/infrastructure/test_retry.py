import httpx
import pytest

from incident_insights.application.ports import IncidentSourceError
from incident_insights.infrastructure.incidents.acl import IncidentTranslator
from incident_insights.infrastructure.incidents.api_source import ApiIncidentSource
from incident_insights.infrastructure.incidents.retry import (
    RetryingGetter,
    RetryPolicy,
    full_jitter,
)
from tests.builders import make_record, payload_json, window_of

POLICY = RetryPolicy(attempts=3, base_delay_seconds=0.2, max_delay_seconds=0.3)


class Scripted:
    def __init__(self, *outcomes: int | Exception) -> None:
        self.outcomes = list(outcomes)
        self.calls = 0

    def __call__(self, _request: httpx.Request) -> httpx.Response:
        self.calls += 1
        outcome = self.outcomes.pop(0)
        if isinstance(outcome, Exception):
            raise outcome
        if outcome == 200:
            return httpx.Response(200, json=[payload_json(make_record())])
        return httpx.Response(outcome, json={"title": "error"})


def _getter(script: Scripted, sleeps: list[float]) -> RetryingGetter:
    client = httpx.Client(base_url="https://incidents.test", transport=httpx.MockTransport(script))
    return RetryingGetter(client, POLICY, sleep=sleeps.append, jitter=lambda ceiling: ceiling)


def _timeout() -> httpx.ReadTimeout:
    return httpx.ReadTimeout("slow", request=httpx.Request("GET", "https://incidents.test"))


def test_retries_server_errors_with_exponential_backoff_until_success() -> None:
    script = Scripted(502, 503, 200)
    sleeps: list[float] = []

    response = _getter(script, sleeps).get("/api/incidents/export", None)

    assert response.status_code == 200
    assert script.calls == 3
    assert sleeps == [0.2, 0.3]


def test_retries_timeouts_and_connection_errors() -> None:
    connect = httpx.ConnectError("refused", request=httpx.Request("GET", "https://incidents.test"))
    script = Scripted(_timeout(), connect, 200)

    response = _getter(script, []).get("/api/incidents/export", None)

    assert response.status_code == 200
    assert script.calls == 3


def test_client_errors_are_not_retried() -> None:
    script = Scripted(404)
    sleeps: list[float] = []

    response = _getter(script, sleeps).get("/api/incidents/x", None)

    assert response.status_code == 404
    assert script.calls == 1
    assert sleeps == []


def test_exhausted_server_errors_surface_as_source_error() -> None:
    script = Scripted(500, 502, 503)
    source = ApiIncidentSource(_getter(script, []), IncidentTranslator())

    with pytest.raises(IncidentSourceError, match="503"):
        source.export(window_of(1))
    assert script.calls == 3


def test_exhausted_timeouts_surface_as_source_error() -> None:
    script = Scripted(_timeout(), _timeout(), _timeout())
    source = ApiIncidentSource(_getter(script, []), IncidentTranslator())

    with pytest.raises(IncidentSourceError, match="unreachable"):
        source.export(window_of(1))
    assert script.calls == 3


def test_single_attempt_policy_does_not_retry() -> None:
    script = Scripted(503)
    client = httpx.Client(base_url="https://incidents.test", transport=httpx.MockTransport(script))
    getter = RetryingGetter(client, RetryPolicy(attempts=1), sleep=lambda _delay: None)

    assert getter.get("/api/incidents/export", None).status_code == 503
    assert script.calls == 1


def test_full_jitter_stays_within_ceiling() -> None:
    assert all(0.0 <= full_jitter(0.5) <= 0.5 for _ in range(50))
