from dataclasses import dataclass

from incident_insights.application.build_ktlo_report import BuildKtloReport
from incident_insights.application.detect_volume_anomalies import DetectVolumeAnomalies
from incident_insights.application.draft_rca import DraftRca
from incident_insights.application.find_recurring_issues import FindRecurringIssues
from incident_insights.application.get_kpis import GetKpis


@dataclass(frozen=True)
class UseCases:
    get_kpis: GetKpis
    find_recurring: FindRecurringIssues
    detect_anomalies: DetectVolumeAnomalies
    draft_rca: DraftRca
    build_report: BuildKtloReport
