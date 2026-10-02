import time
from collections.abc import Callable
from datetime import datetime, timedelta
from threading import Lock
from uuid import UUID

from incident_insights.application.ports import IncidentSource
from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.shared.reporting_window import ReportingWindow

_HOUR = timedelta(hours=1)


class TtlCache[K, V]:
    def __init__(self, ttl_seconds: float, clock: Callable[[], float]) -> None:
        self._ttl = ttl_seconds
        self._clock = clock
        self._entries: dict[K, tuple[float, V]] = {}
        self._lock = Lock()

    def get_or_load(self, key: K, loader: Callable[[], V]) -> V:
        now = self._clock()
        with self._lock:
            cached = self._entries.get(key)
        if cached is not None and now - cached[0] < self._ttl:
            return cached[1]
        value = loader()
        with self._lock:
            self._entries[key] = (now, value)
        return value


class CachedIncidentSource:
    def __init__(
        self,
        inner: IncidentSource,
        ttl_seconds: float,
        clock: Callable[[], float] = time.monotonic,
    ) -> None:
        self._inner = inner
        self._exports: TtlCache[ReportingWindow, IncidentHistory] = TtlCache(ttl_seconds, clock)

    def export(self, window: ReportingWindow) -> IncidentHistory:
        aligned = ReportingWindow(_floor_hour(window.start), _floor_hour(window.end) + _HOUR)
        history = self._exports.get_or_load(aligned, lambda: self._inner.export(aligned))
        return history.within(window)

    def get(self, incident_id: UUID) -> IncidentRecord | None:
        return self._inner.get(incident_id)


def _floor_hour(moment: datetime) -> datetime:
    return moment.replace(minute=0, second=0, microsecond=0)
