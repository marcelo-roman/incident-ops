import logging

from azure.monitor.opentelemetry import configure_azure_monitor
from opentelemetry.instrumentation.httpx import HTTPXClientInstrumentor
from opentelemetry.sdk.resources import SERVICE_NAME, Resource

from incident_insights.infrastructure.settings import Settings

logger = logging.getLogger(__name__)


def configure_telemetry(settings: Settings) -> bool:
    connection_string = settings.applicationinsights_connection_string
    if not connection_string:
        logger.info("telemetry disabled", extra={"reason": "no connection string"})
        return False
    configure_azure_monitor(
        connection_string=connection_string,
        resource=Resource.create({SERVICE_NAME: settings.otel_service_name}),
        logger_name="incident_insights",
    )
    HTTPXClientInstrumentor().instrument()
    logger.info("telemetry enabled", extra={"exporter": "azure-monitor"})
    return True
