from collections import Counter
from collections.abc import Callable, Iterable, Iterator
from dataclasses import dataclass
from datetime import date
from uuid import UUID

from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.reporting_window import ReportingWindow
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.domain.shared.severity import Severity


@dataclass(frozen=True, slots=True)
class WeeklyVolume:
    weeks: tuple[date, ...]
    counts: dict[ServiceId, tuple[int, ...]]

    def __post_init__(self) -> None:
        if any(len(series) != len(self.weeks) for series in self.counts.values()):
            raise ValueError("every service needs one count per week")


class IncidentHistory:
    __slots__ = ("_records",)

    def __init__(self, records: Iterable[IncidentRecord] = ()) -> None:
        self._records = tuple(records)

    def __iter__(self) -> Iterator[IncidentRecord]:
        return iter(self._records)

    def __len__(self) -> int:
        return len(self._records)

    def __eq__(self, other: object) -> bool:
        if not isinstance(other, IncidentHistory):
            return NotImplemented
        return self._records == other._records

    def __hash__(self) -> int:
        return hash(self._records)

    @property
    def is_empty(self) -> bool:
        return not self._records

    def find(self, incident_id: UUID) -> IncidentRecord | None:
        return next((record for record in self._records if record.id == incident_id), None)

    def within(self, window: ReportingWindow) -> "IncidentHistory":
        return self.where(lambda record: window.contains(record.created_at))

    def where(self, predicate: Callable[[IncidentRecord], bool]) -> "IncidentHistory":
        return IncidentHistory(record for record in self._records if predicate(record))

    def in_week(self, week: date) -> "IncidentHistory":
        return self.where(lambda record: record.week_start() == week)

    def count(self, predicate: Callable[[IncidentRecord], bool]) -> int:
        return sum(1 for record in self._records if predicate(record))

    def acknowledgement_minutes(self) -> list[float]:
        return [
            minutes
            for record in self._records
            if (minutes := record.minutes_to_acknowledge()) is not None
        ]

    def by_service(self) -> dict[ServiceId, "IncidentHistory"]:
        return _sorted_groups(self._buckets(lambda record: record.service))

    def by_severity(self) -> dict[Severity, "IncidentHistory"]:
        return _sorted_groups(self._buckets(lambda record: record.severity))

    def by_responder(self) -> dict[str, "IncidentHistory"]:
        return _sorted_groups(self._buckets(lambda record: record.responder))

    def by_source(self) -> dict[DetectionSource, "IncidentHistory"]:
        buckets = self._buckets(lambda record: record.source)
        return {source: IncidentHistory(buckets.get(source, [])) for source in DetectionSource}

    def weekly_volume(self, window: ReportingWindow) -> WeeklyVolume:
        weeks = window.week_starts()
        return WeeklyVolume(
            weeks=weeks,
            counts={
                service: _weekly_counts(history, weeks)
                for service, history in self.by_service().items()
            },
        )

    def _buckets[K](self, key: Callable[[IncidentRecord], K]) -> dict[K, list[IncidentRecord]]:
        buckets: dict[K, list[IncidentRecord]] = {}
        for record in self._records:
            buckets.setdefault(key(record), []).append(record)
        return buckets


def _sorted_groups[K: (ServiceId, Severity, str)](
    buckets: dict[K, list[IncidentRecord]],
) -> dict[K, IncidentHistory]:
    return {key: IncidentHistory(buckets[key]) for key in sorted(buckets)}


def _weekly_counts(history: IncidentHistory, weeks: tuple[date, ...]) -> tuple[int, ...]:
    counts = Counter(record.week_start() for record in history)
    return tuple(counts[week] for week in weeks)
