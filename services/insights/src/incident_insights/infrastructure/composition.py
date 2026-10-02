from incident_insights.application.build_ktlo_report import BuildKtloReport
from incident_insights.application.clock import utc_now
from incident_insights.application.detect_volume_anomalies import DetectVolumeAnomalies
from incident_insights.application.draft_rca import DraftRca
from incident_insights.application.find_recurring_issues import FindRecurringIssues
from incident_insights.application.get_kpis import GetKpis
from incident_insights.application.history_loader import HistoryLoader
from incident_insights.application.ports import Clock
from incident_insights.application.use_cases import UseCases
from incident_insights.domain.anomalies.volume_anomaly_detector import VolumeAnomalyDetector
from incident_insights.domain.kpis.kpi_calculator import KpiCalculator
from incident_insights.domain.recurring.recurring_issue_detector import RecurringIssueDetector
from incident_insights.domain.reports.ktlo_report_composer import KtloReportComposer
from incident_insights.infrastructure.incidents.factory import build_source
from incident_insights.infrastructure.rca.factory import build_rca_drafter
from incident_insights.infrastructure.settings import Settings


def build_use_cases(settings: Settings, clock: Clock = utc_now) -> UseCases:
    source = build_source(settings)
    loader = HistoryLoader(source, clock)
    kpis = KpiCalculator()
    recurring = RecurringIssueDetector()
    return UseCases(
        get_kpis=GetKpis(loader, kpis),
        find_recurring=FindRecurringIssues(loader, recurring),
        detect_anomalies=DetectVolumeAnomalies(loader, VolumeAnomalyDetector()),
        draft_rca=DraftRca(source, build_rca_drafter(settings), clock),
        build_report=BuildKtloReport(loader, KtloReportComposer(kpis, recurring)),
    )
