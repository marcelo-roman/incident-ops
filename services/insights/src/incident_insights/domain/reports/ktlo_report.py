from dataclasses import dataclass
from datetime import datetime

from incident_insights.domain.incidents.status import IncidentStatus
from incident_insights.domain.kpis.report import DetectionCoverage, KpiGroup, KpiSummary
from incident_insights.domain.recurring.cluster import Cluster
from incident_insights.domain.shared.reporting_window import ReportingWindow
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.domain.shared.severity import Severity


@dataclass(frozen=True, slots=True)
class SlaBreach:
    number: int
    title: str
    service: ServiceId
    severity: Severity
    status: IncidentStatus
    assignee: str | None
    breached_targets: tuple[str, ...]
    resolve_overrun_minutes: float | None

    def __post_init__(self) -> None:
        if not self.breached_targets:
            raise ValueError("a breach names at least one missed target")


@dataclass(frozen=True, slots=True)
class OnCallLoad:
    assignee: str
    incidents: int
    high_severity: int
    off_hours: int
    escalated: int

    def __post_init__(self) -> None:
        if max(self.high_severity, self.off_hours, self.escalated) > self.incidents:
            raise ValueError("load breakdowns cannot exceed the incident count")


@dataclass(frozen=True, slots=True)
class KtloReport:
    generated_at: datetime
    window: ReportingWindow
    overall: KpiSummary
    detection: DetectionCoverage
    top_recurring: tuple[Cluster, ...]
    breaches: tuple[SlaBreach, ...]
    total_breaches: int
    mttr_by_service: tuple[KpiGroup, ...]
    on_call: tuple[OnCallLoad, ...]
