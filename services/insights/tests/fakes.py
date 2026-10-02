from uuid import UUID

from incident_insights.application.ports import IncidentSourceError, RcaDraftError
from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.rca.draft import RcaDraft
from incident_insights.domain.shared.reporting_window import ReportingWindow


class InMemoryIncidentSource:
    def __init__(self, records: list[IncidentRecord]) -> None:
        self.history = IncidentHistory(records)
        self.export_calls: list[ReportingWindow] = []

    def export(self, window: ReportingWindow) -> IncidentHistory:
        self.export_calls.append(window)
        return self.history.within(window)

    def get(self, incident_id: UUID) -> IncidentRecord | None:
        return self.history.find(incident_id)


class UnavailableIncidentSource:
    def export(self, _window: ReportingWindow) -> IncidentHistory:
        raise IncidentSourceError("Incidents API is unreachable")

    def get(self, _incident_id: UUID) -> IncidentRecord | None:
        raise IncidentSourceError("Incidents API is unreachable")


class CannedRcaDrafter:
    def __init__(self, draft: RcaDraft) -> None:
        self._draft = draft
        self.received: list[IncidentRecord] = []

    @property
    def name(self) -> str:
        return "canned"

    def draft(self, incident: IncidentRecord) -> RcaDraft:
        self.received.append(incident)
        return self._draft


class FailingRcaDrafter:
    @property
    def name(self) -> str:
        return "failing"

    def draft(self, _incident: IncidentRecord) -> RcaDraft:
        raise RcaDraftError("model unavailable")
