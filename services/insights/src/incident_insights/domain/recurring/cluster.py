from dataclasses import dataclass
from datetime import datetime

from incident_insights.domain.shared.reporting_window import ReportingWindow
from incident_insights.domain.shared.service_id import ServiceId


@dataclass(frozen=True, slots=True)
class ServiceCount:
    service: ServiceId
    count: int


@dataclass(frozen=True, slots=True)
class Cluster:
    terms: tuple[str, ...]
    count: int
    cohesion: float
    services: tuple[ServiceCount, ...]
    sample_titles: tuple[str, ...]
    first_seen: datetime
    last_seen: datetime

    def __post_init__(self) -> None:
        if not self.terms:
            raise ValueError("a cluster is labelled by at least one term")
        if self.count < 1:
            raise ValueError("a cluster holds at least one incident")
        if self.first_seen > self.last_seen:
            raise ValueError("first occurrence cannot follow the last one")

    @property
    def label(self) -> str:
        return " / ".join(self.terms)

    def is_recurring(self, min_size: int) -> bool:
        return self.count >= min_size


@dataclass(frozen=True, slots=True)
class RecurringReport:
    window: ReportingWindow
    incidents_analyzed: int
    clusters_evaluated: int
    silhouette: float | None
    clusters: tuple[Cluster, ...]

    @classmethod
    def empty(cls, window: ReportingWindow, analyzed: int) -> "RecurringReport":
        return cls(window, analyzed, 0, None, ())

    def top(self, limit: int) -> tuple[Cluster, ...]:
        return self.clusters[:limit]
