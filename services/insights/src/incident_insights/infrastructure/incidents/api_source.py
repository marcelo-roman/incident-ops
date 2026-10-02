from datetime import UTC, datetime
from uuid import UUID

import httpx
from pydantic import TypeAdapter, ValidationError

from incident_insights.application.ports import IncidentSourceError
from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.shared.reporting_window import ReportingWindow
from incident_insights.infrastructure.incidents.acl import (
    IncidentDetailPayload,
    IncidentPayload,
    IncidentTranslator,
)
from incident_insights.infrastructure.incidents.retry import RetryingGetter

_EXPORT = TypeAdapter(list[IncidentPayload])


class ApiIncidentSource:
    def __init__(self, getter: RetryingGetter, translator: IncidentTranslator) -> None:
        self._getter = getter
        self._translator = translator

    def export(self, window: ReportingWindow) -> IncidentHistory:
        params = {"from": _iso(window.start), "to": _iso(window.end)}
        response = self._request("/api/incidents/export", params)
        try:
            payloads = _EXPORT.validate_json(response.content)
            return IncidentHistory(self._translator.to_record(item) for item in payloads)
        except (ValidationError, ValueError) as error:
            raise IncidentSourceError("Incidents API returned an invalid export") from error

    def get(self, incident_id: UUID) -> IncidentRecord | None:
        response = self._request(f"/api/incidents/{incident_id}", None)
        if response.status_code == httpx.codes.NOT_FOUND:
            return None
        try:
            return self._translator.to_record(
                IncidentDetailPayload.model_validate_json(response.content)
            )
        except (ValidationError, ValueError) as error:
            raise IncidentSourceError("Incidents API returned an invalid incident") from error

    def _request(self, path: str, params: dict[str, str] | None) -> httpx.Response:
        try:
            response = self._getter.get(path, params)
        except httpx.HTTPError as error:
            raise IncidentSourceError(f"Incidents API is unreachable: {error}") from error
        if response.is_success or response.status_code == httpx.codes.NOT_FOUND:
            return response
        raise IncidentSourceError(f"Incidents API answered {response.status_code} for {path}")


API_KEY_HEADER = "X-Api-Key"


def create_api_client(base_url: str, timeout_seconds: float, api_key: str) -> httpx.Client:
    return httpx.Client(
        base_url=base_url,
        timeout=timeout_seconds,
        headers=_headers(api_key),
    )


def _headers(api_key: str) -> dict[str, str]:
    headers = {"Accept": "application/json", "User-Agent": "incident-ops-insights"}
    if not api_key:
        return headers
    return {**headers, API_KEY_HEADER: api_key}


def _iso(moment: datetime) -> str:
    return moment.astimezone(UTC).strftime("%Y-%m-%dT%H:%M:%SZ")
