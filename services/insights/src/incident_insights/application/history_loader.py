from incident_insights.application.ports import Clock, IncidentSource
from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.shared.reporting_window import ReportingWindow


class HistoryLoader:
    def __init__(self, source: IncidentSource, clock: Clock) -> None:
        self._source = source
        self._clock = clock

    def trailing(self, days: int) -> tuple[IncidentHistory, ReportingWindow]:
        window = ReportingWindow.trailing(days, self._clock())
        return self._source.export(window), window
