from enum import StrEnum
from pathlib import Path
from typing import Annotated

from pydantic import Field, SecretStr, field_validator
from pydantic_settings import BaseSettings, NoDecode, SettingsConfigDict

DEFAULT_CORS_ORIGINS = ("https://incidents.marceloroman.com.br", "http://localhost:5173")


class DataSourceKind(StrEnum):
    API = "api"
    CSV = "csv"


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    incidents_api_base_url: str = "https://incidents-api.marceloroman.com.br"
    incidents_api_timeout_seconds: float = 15.0
    incidents_api_attempts: int = Field(default=3, ge=1)
    incidents_api_key: SecretStr | None = None
    auth_signing_key: SecretStr | None = None
    insights_data_source: DataSourceKind = DataSourceKind.API
    insights_csv_path: Path = Path("data/sample_incidents.csv")
    insights_cache_ttl_seconds: float = Field(default=300.0, ge=0)
    cors_allowed_origins: Annotated[list[str], NoDecode] = list(DEFAULT_CORS_ORIGINS)
    azure_openai_endpoint: str | None = None
    azure_openai_deployment: str = "rca-drafts"
    azure_openai_api_version: str = "2025-04-01-preview"
    applicationinsights_connection_string: str | None = None
    otel_service_name: str = "incident-ops-insights"
    log_level: str = "INFO"

    @field_validator("cors_allowed_origins", mode="before")
    @classmethod
    def _split_origins(cls, value: object) -> object:
        if not isinstance(value, str):
            return value
        return [origin.strip() for origin in value.split(",") if origin.strip()]
