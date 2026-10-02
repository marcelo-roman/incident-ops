import pytest

from incident_insights.infrastructure.settings import DataSourceKind, Settings


def test_defaults_follow_the_runtime_contract() -> None:
    settings = Settings()

    assert settings.incidents_api_base_url == "https://incidents-api.marceloroman.com.br"
    assert settings.azure_openai_deployment == "rca-drafts"
    assert settings.azure_openai_endpoint is None
    assert settings.otel_service_name == "incident-ops-insights"
    assert settings.cors_allowed_origins == [
        "https://incidents.marceloroman.com.br",
        "http://localhost:5173",
    ]


def test_cors_origins_are_read_comma_separated(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("CORS_ALLOWED_ORIGINS", "https://a.example, https://b.example,")

    assert Settings().cors_allowed_origins == ["https://a.example", "https://b.example"]


def test_environment_selects_source_and_model(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("INSIGHTS_DATA_SOURCE", "csv")
    monkeypatch.setenv("AZURE_OPENAI_ENDPOINT", "https://example.openai.azure.com")
    monkeypatch.setenv("AZURE_OPENAI_DEPLOYMENT", "rca-drafts-canary")

    settings = Settings()

    assert settings.insights_data_source is DataSourceKind.CSV
    assert settings.azure_openai_deployment == "rca-drafts-canary"
