from incident_insights.application.history_loader import HistoryLoader
from incident_insights.application.outputs.kpis import KpiReportOutput
from incident_insights.domain.kpis.kpi_calculator import KpiCalculator


class GetKpis:
    def __init__(self, loader: HistoryLoader, calculator: KpiCalculator) -> None:
        self._loader = loader
        self._calculator = calculator

    def execute(self, days: int) -> KpiReportOutput:
        history, window = self._loader.trailing(days)
        return KpiReportOutput.of(self._calculator.report(history, window))
