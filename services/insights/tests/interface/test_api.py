from datetime import UTC, datetime, timedelta
from pathlib import Path
from uuid import uuid4

import pytest
from fastapi.testclient import TestClient

from incident_insights.application.build_ktlo_report import BuildKtloReport
from incident_insights.application.clock import fixed_clock
from incident_insights.application.detect_volume_anomalies import DetectVolumeAnomalies
from incident_insights.application.draft_rca import DraftRca
from incident_insights.application.find_recurring_issues import FindRecurringIssues
from incident_insights.application.get_kpis import GetKpis
from incident_insights.application.history_loader import HistoryLoader
from incident_insights.application.ports import IncidentSource, RcaDrafter
from incident_insights.application.use_cases import UseCases
from incident_insights.domain.anomalies.volume_anomaly_detector import VolumeAnomalyDetector
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.kpis.kpi_calculator import KpiCalculator
from incident_insights.domain.rca.rca_draft_composer import RcaDraftComposer
from incident_insights.domain.recurring.recurring_issue_detector import RecurringIssueDetector
from incident_insights.domain.reports.ktlo_report_composer import KtloReportComposer
from incident_insights.infrastructure.composition import build_use_cases
from incident_insights.infrastructure.rca.fallback_drafter import DeterministicRcaDrafter
from incident_insights.infrastructure.settings import DataSourceKind, Settings
from incident_insights.interface.api.app import create_app
from tests.builders import AS_OF, make_record
from tests.fakes import FailingRcaDrafter, InMemoryIncidentSource, UnavailableIncidentSource
from tests.tokens import authorized, signing_key

ORIGIN = "https://incidents.marceloroman.com.br"


def _use_cases(source: IncidentSource, drafter: RcaDrafter) -> UseCases:
    clock = fixed_clock(AS_OF)
    loader = HistoryLoader(source, clock)
    kpis = KpiCalculator()
    recurring = RecurringIssueDetector()
    return UseCases(
        get_kpis=GetKpis(loader, kpis),
        find_recurring=FindRecurringIssues(loader, recurring),
        detect_anomalies=DetectVolumeAnomalies(loader, VolumeAnomalyDetector()),
        draft_rca=DraftRca(source, drafter, clock),
        build_report=BuildKtloReport(loader, KtloReportComposer(kpis, recurring)),
    )


def _client(source: IncidentSource, drafter: RcaDrafter | None = None) -> TestClient:
    use_cases = _use_cases(source, drafter or DeterministicRcaDrafter(RcaDraftComposer()))
    return authorized(TestClient(create_app(Settings(auth_signing_key=signing_key()), use_cases)))


def _incidents() -> list[IncidentRecord]:
    return [
        make_record(number=index, created_at=AS_OF - timedelta(days=index + 1))
        for index in range(6)
    ]


@pytest.fixture
def client() -> TestClient:
    return _client(InMemoryIncidentSource(_incidents()))


def test_health(client: TestClient) -> None:
    response = client.get("/health")

    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


def test_kpis_returns_camel_case_report(client: TestClient) -> None:
    response = client.get("/api/kpis", params={"days": 30})

    body = response.json()
    assert response.status_code == 200
    assert body["window"]["days"] == 30
    assert body["overall"]["incidents"] == 6
    assert body["overall"]["mtta"]["medianMinutes"] == 10
    assert body["bySeverity"][0]["severity"] == "Sev2"
    assert "alertingPct" in body["detection"]


def test_recurring_and_anomalies_use_default_windows(client: TestClient) -> None:
    recurring = client.get("/api/recurring").json()
    anomalies = client.get("/api/anomalies").json()

    assert recurring["window"]["days"] == 180
    assert recurring["incidentsAnalyzed"] == 6
    assert anomalies["window"]["days"] == 180
    assert anomalies["method"] == "rolling-z-score"


@pytest.mark.parametrize("days", [0, 731, "many"])
def test_invalid_days_is_a_problem_response(client: TestClient, days: int | str) -> None:
    response = client.get("/api/kpis", params={"days": days})

    assert response.status_code == 422
    assert response.headers["content-type"] == "application/problem+json"
    assert response.json()["errors"][0]["loc"] == ["query", "days"]


def test_rca_draft_is_labelled_with_its_generator() -> None:
    incident = make_record(number=1042)
    client = _client(InMemoryIncidentSource([incident]))

    response = client.post("/api/rca/draft", json={"incidentId": str(incident.id)})

    body = response.json()
    assert response.status_code == 200
    assert body["generatedBy"] == "deterministic-fallback"
    assert body["incidentNumber"] == 1042
    assert {"summary", "impact", "timeline", "contributingFactors", "actionItems"} <= set(body)


def test_rca_draft_for_unknown_incident_is_not_found(client: TestClient) -> None:
    response = client.post("/api/rca/draft", json={"incidentId": str(uuid4())})

    assert response.status_code == 404
    assert response.json()["title"] == "Not Found"


def test_rca_draft_requires_incident_id(client: TestClient) -> None:
    assert client.post("/api/rca/draft", json={}).status_code == 422


def test_unavailable_source_is_bad_gateway() -> None:
    client = _client(UnavailableIncidentSource())

    response = client.get("/api/kpis")

    assert response.status_code == 502
    assert response.json()["detail"] == "Incidents API is unreachable"


def test_failing_model_is_bad_gateway() -> None:
    incident = make_record()
    client = _client(InMemoryIncidentSource([incident]), FailingRcaDrafter())

    response = client.post("/api/rca/draft", json={"incidentId": str(incident.id)})

    assert response.status_code == 502


def test_cors_allows_console_origin(client: TestClient) -> None:
    response = client.options(
        "/api/kpis",
        headers={"Origin": ORIGIN, "Access-Control-Request-Method": "GET"},
    )

    assert response.headers["access-control-allow-origin"] == ORIGIN


def test_cors_allows_authorization_header(client: TestClient) -> None:
    response = client.options(
        "/api/kpis",
        headers={
            "Origin": ORIGIN,
            "Access-Control-Request-Method": "GET",
            "Access-Control-Request-Headers": "authorization",
        },
    )

    assert response.status_code == 200
    assert "authorization" in response.headers["access-control-allow-headers"].lower()


def test_cors_rejects_unknown_origin(client: TestClient) -> None:
    response = client.get("/health", headers={"Origin": "https://evil.example"})

    assert "access-control-allow-origin" not in response.headers


def test_correlation_id_is_echoed_or_generated(client: TestClient) -> None:
    echoed = client.get("/health", headers={"X-Correlation-Id": "abc-123"})
    generated = client.get("/health")

    assert echoed.headers["x-correlation-id"] == "abc-123"
    assert len(generated.headers["x-correlation-id"]) == 32


def _csv_settings(sample_csv: Path) -> Settings:
    return Settings(
        insights_data_source=DataSourceKind.CSV,
        insights_csv_path=sample_csv,
        auth_signing_key=signing_key(),
    )


def test_default_container_reads_sample_csv(sample_csv: Path) -> None:
    settings = _csv_settings(sample_csv)
    use_cases = build_use_cases(settings, fixed_clock(datetime(2026, 9, 28, tzinfo=UTC)))
    client = authorized(TestClient(create_app(settings, use_cases)))

    assert client.get("/api/kpis", params={"days": 30}).json()["overall"]["incidents"] > 0


def test_create_app_builds_container_from_settings(sample_csv: Path) -> None:
    client = TestClient(create_app(_csv_settings(sample_csv)))

    assert client.get("/health").status_code == 200
