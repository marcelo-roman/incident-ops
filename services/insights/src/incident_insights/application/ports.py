from collections.abc import Callable
from datetime import datetime
from typing import Protocol
from uuid import UUID

from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.rca.draft import RcaDraft
from incident_insights.domain.shared.reporting_window import ReportingWindow

type Clock = Callable[[], datetime]


class IncidentSourceError(Exception):
    pass


class RcaDraftError(Exception):
    pass


class IncidentSource(Protocol):
    def export(self, window: ReportingWindow) -> IncidentHistory: ...

    def get(self, incident_id: UUID) -> IncidentRecord | None: ...


class RcaDrafter(Protocol):
    @property
    def name(self) -> str: ...

    def draft(self, incident: IncidentRecord) -> RcaDraft: ...
