from pathlib import Path

import pytest

SAMPLE_CSV = Path(__file__).resolve().parents[1] / "data" / "sample_incidents.csv"

ISOLATED_VARIABLES = (
    "AZURE_OPENAI_ENDPOINT",
    "AZURE_OPENAI_DEPLOYMENT",
    "APPLICATIONINSIGHTS_CONNECTION_STRING",
    "INCIDENTS_API_BASE_URL",
    "INSIGHTS_DATA_SOURCE",
    "INSIGHTS_CSV_PATH",
    "CORS_ALLOWED_ORIGINS",
    "AZURE_OPENAI_API_VERSION",
    "OTEL_SERVICE_NAME",
    "AUTH_SIGNING_KEY",
    "INCIDENTS_API_KEY",
)


@pytest.fixture(autouse=True)
def isolated_environment(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    for variable in ISOLATED_VARIABLES:
        monkeypatch.delenv(variable, raising=False)
    monkeypatch.chdir(tmp_path)


@pytest.fixture
def sample_csv() -> Path:
    return SAMPLE_CSV
