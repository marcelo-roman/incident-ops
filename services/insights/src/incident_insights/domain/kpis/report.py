from dataclasses import dataclass
from datetime import date

from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.duration_stats import DurationStats
from incident_insights.domain.shared.percentage import Percentage
from incident_insights.domain.shared.reporting_window import ReportingWindow
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.domain.shared.severity import Severity


@dataclass(frozen=True, slots=True)
class SlaCompliance:
    evaluated: int
    breached: int
    acknowledgement: Percentage | None
    resolution: Percentage | None
    overall: Percentage | None

    def __post_init__(self) -> None:
        if not 0 <= self.breached <= self.evaluated:
            raise ValueError("breaches must be part of the evaluated incidents")


@dataclass(frozen=True, slots=True)
class KpiSummary:
    incidents: int
    time_to_acknowledge: DurationStats
    time_to_resolve: DurationStats
    sla: SlaCompliance


@dataclass(frozen=True, slots=True)
class KpiGroup:
    summary: KpiSummary
    service: ServiceId | None = None
    severity: Severity | None = None


@dataclass(frozen=True, slots=True)
class WeeklyKpi:
    week_start: date
    summary: KpiSummary


@dataclass(frozen=True, slots=True)
class SourceKpi:
    source: DetectionSource
    incidents: int
    share: Percentage | None
    time_to_acknowledge: DurationStats


@dataclass(frozen=True, slots=True)
class DetectionCoverage:
    incidents: int
    monitoring_share: Percentage | None
    by_source: tuple[SourceKpi, ...]


@dataclass(frozen=True, slots=True)
class KpiReport:
    window: ReportingWindow
    overall: KpiSummary
    detection: DetectionCoverage
    by_service: tuple[KpiGroup, ...]
    by_severity: tuple[KpiGroup, ...]
    by_service_severity: tuple[KpiGroup, ...]
    weekly: tuple[WeeklyKpi, ...]
