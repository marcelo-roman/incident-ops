from incident_insights.application.history_loader import HistoryLoader
from incident_insights.application.outputs.reports import KtloReportOutput
from incident_insights.domain.reports.ktlo_report_composer import KtloReportComposer


class BuildKtloReport:
    def __init__(self, loader: HistoryLoader, composer: KtloReportComposer) -> None:
        self._loader = loader
        self._composer = composer

    def execute(self, days: int) -> KtloReportOutput:
        history, window = self._loader.trailing(days)
        return KtloReportOutput.of(self._composer.compose(history, window))
