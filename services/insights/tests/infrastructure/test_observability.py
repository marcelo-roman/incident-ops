import json
import logging
import sys

import pytest

from incident_insights.infrastructure.observability import telemetry
from incident_insights.infrastructure.observability.correlation import correlation_id
from incident_insights.infrastructure.observability.logging import JsonFormatter, configure_logging
from incident_insights.infrastructure.settings import Settings


class FakeInstrumentor:
    def instrument(self) -> None:
        return None


def _record(**extra: object) -> logging.LogRecord:
    record = logging.makeLogRecord({"name": "test", "levelname": "INFO", "msg": "hello %s"})
    record.args = ("world",)
    for key, value in extra.items():
        setattr(record, key, value)
    return record


def test_json_formatter_includes_correlation_id_and_extras() -> None:
    token = correlation_id.set("corr-1")
    try:
        entry = json.loads(JsonFormatter().format(_record(path="/api/kpis")))
    finally:
        correlation_id.reset(token)

    assert entry["message"] == "hello world"
    assert entry["correlationId"] == "corr-1"
    assert entry["path"] == "/api/kpis"
    assert "traceId" not in entry


def test_json_formatter_includes_exceptions() -> None:
    try:
        raise ValueError("boom")
    except ValueError:
        record = logging.makeLogRecord({"msg": "failed", "exc_info": sys.exc_info()})

    entry = json.loads(JsonFormatter().format(record))

    assert "ValueError: boom" in entry["exception"]


def test_configure_logging_replaces_root_handlers() -> None:
    configure_logging("debug")
    configure_logging("info")

    root = logging.getLogger()
    assert len(root.handlers) == 1
    assert isinstance(root.handlers[0].formatter, JsonFormatter)
    assert root.level == logging.INFO


def test_telemetry_disabled_without_connection_string() -> None:
    assert telemetry.configure_telemetry(Settings()) is False


def test_telemetry_configures_azure_monitor(monkeypatch: pytest.MonkeyPatch) -> None:
    calls: dict[str, object] = {}
    monkeypatch.setattr(telemetry, "configure_azure_monitor", lambda **kwargs: calls.update(kwargs))
    monkeypatch.setattr(telemetry, "HTTPXClientInstrumentor", FakeInstrumentor)
    settings = Settings(applicationinsights_connection_string="InstrumentationKey=test")

    assert telemetry.configure_telemetry(settings) is True
    assert calls["connection_string"] == "InstrumentationKey=test"
    assert calls["logger_name"] == "incident_insights"
