from incident_insights.application.history_loader import HistoryLoader
from incident_insights.application.outputs.recurring import RecurringReportOutput
from incident_insights.domain.recurring.recurring_issue_detector import RecurringIssueDetector


class FindRecurringIssues:
    def __init__(self, loader: HistoryLoader, detector: RecurringIssueDetector) -> None:
        self._loader = loader
        self._detector = detector

    def execute(self, days: int) -> RecurringReportOutput:
        history, window = self._loader.trailing(days)
        return RecurringReportOutput.of(self._detector.detect(history, window))
