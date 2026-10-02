from dataclasses import dataclass
from datetime import date

from incident_insights.domain.shared.reporting_window import ReportingWindow
from incident_insights.domain.shared.service_id import ServiceId


@dataclass(frozen=True, slots=True)
class VolumeAnomaly:
    service: ServiceId
    week_start: date
    count: int
    baseline_mean: float
    baseline_std: float
    z_score: float

    def __post_init__(self) -> None:
        if self.count < 0 or self.baseline_std < 0:
            raise ValueError("counts and deviations cannot be negative")


@dataclass(frozen=True, slots=True)
class AnomalyReport:
    window: ReportingWindow
    method: str
    baseline_weeks: int
    threshold: float
    anomalies: tuple[VolumeAnomaly, ...]
