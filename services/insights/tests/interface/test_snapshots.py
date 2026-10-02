import json
from datetime import UTC, datetime
from pathlib import Path

import pytest
from fastapi.testclient import TestClient
from typer.testing import CliRunner

from incident_insights.application.clock import fixed_clock
from incident_insights.infrastructure.composition import build_use_cases
from incident_insights.infrastructure.settings import DataSourceKind, Settings
from incident_insights.interface.api.app import create_app
from incident_insights.interface.cli.app import app as cli

SNAPSHOTS = Path(__file__).resolve().parents[1] / "snapshots"
SAMPLE_END = datetime(2026, 9, 28, tzinfo=UTC)
REQUESTS = {
    "kpis_90": ("/api/kpis", 90),
    "kpis_7": ("/api/kpis", 7),
    "recurring_180": ("/api/recurring", 180),
    "recurring_30": ("/api/recurring", 30),
    "anomalies_180": ("/api/anomalies", 180),
}


@pytest.fixture
def client(sample_csv: Path) -> TestClient:
    settings = Settings(insights_data_source=DataSourceKind.CSV, insights_csv_path=sample_csv)
    return TestClient(create_app(settings, build_use_cases(settings, fixed_clock(SAMPLE_END))))


def _snapshot(name: str) -> object:
    return json.loads((SNAPSHOTS / f"{name}.json").read_text(encoding="utf-8"))


@pytest.mark.parametrize("name", sorted(REQUESTS))
def test_analytics_responses_match_snapshots(client: TestClient, name: str) -> None:
    path, days = REQUESTS[name]

    response = client.get(path, params={"days": days})

    assert response.json() == _snapshot(name)


def test_rca_drafts_match_snapshots(client: TestClient) -> None:
    expected = _snapshot("rca")
    assert isinstance(expected, dict)

    for incident_id, draft in expected.items():
        body = client.post("/api/rca/draft", json={"incidentId": incident_id}).json()
        body.pop("generatedAt")
        assert body == draft


@pytest.mark.parametrize("report_format", ["md", "html"])
def test_cli_report_matches_snapshot(sample_csv: Path, report_format: str) -> None:
    arguments = ["report", "--source", "csv", "--csv-path", str(sample_csv), "--days", "30"]
    arguments += ["--as-of", "2026-09-28", "--format", report_format]

    result = CliRunner().invoke(cli, arguments)

    expected = (SNAPSHOTS / f"report_30.{report_format}").read_text(encoding="utf-8")
    assert result.exit_code == 0
    assert result.stdout == expected
