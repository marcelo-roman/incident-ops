import httpx

from incident_insights.application.ports import IncidentSource
from incident_insights.infrastructure.incidents.acl import IncidentTranslator
from incident_insights.infrastructure.incidents.api_source import (
    ApiIncidentSource,
    create_api_client,
)
from incident_insights.infrastructure.incidents.cache import CachedIncidentSource
from incident_insights.infrastructure.incidents.csv_source import CsvIncidentSource
from incident_insights.infrastructure.incidents.retry import RetryingGetter, RetryPolicy
from incident_insights.infrastructure.settings import DataSourceKind, Settings


def build_source(settings: Settings) -> IncidentSource:
    translator = IncidentTranslator()
    if settings.insights_data_source is DataSourceKind.CSV:
        return CsvIncidentSource(settings.insights_csv_path, translator)
    getter = RetryingGetter(
        build_api_client(settings), RetryPolicy(attempts=settings.incidents_api_attempts)
    )
    source = ApiIncidentSource(getter, translator)
    return CachedIncidentSource(source, settings.insights_cache_ttl_seconds)


def build_api_client(settings: Settings) -> httpx.Client:
    return create_api_client(
        settings.incidents_api_base_url,
        settings.incidents_api_timeout_seconds,
        _api_key(settings),
    )


def _api_key(settings: Settings) -> str:
    if settings.incidents_api_key is None:
        return ""
    return settings.incidents_api_key.get_secret_value()
