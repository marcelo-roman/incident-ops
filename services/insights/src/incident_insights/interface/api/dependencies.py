from typing import Annotated

from fastapi import Depends, Request

from incident_insights.application.detect_volume_anomalies import DetectVolumeAnomalies
from incident_insights.application.draft_rca import DraftRca
from incident_insights.application.find_recurring_issues import FindRecurringIssues
from incident_insights.application.get_kpis import GetKpis
from incident_insights.application.use_cases import UseCases


def _use_cases(request: Request) -> UseCases:
    use_cases: UseCases = request.app.state.use_cases
    return use_cases


def get_kpis(request: Request) -> GetKpis:
    return _use_cases(request).get_kpis


def find_recurring(request: Request) -> FindRecurringIssues:
    return _use_cases(request).find_recurring


def detect_anomalies(request: Request) -> DetectVolumeAnomalies:
    return _use_cases(request).detect_anomalies


def draft_rca(request: Request) -> DraftRca:
    return _use_cases(request).draft_rca


GetKpisDependency = Annotated[GetKpis, Depends(get_kpis)]
FindRecurringDependency = Annotated[FindRecurringIssues, Depends(find_recurring)]
DetectAnomaliesDependency = Annotated[DetectVolumeAnomalies, Depends(detect_anomalies)]
DraftRcaDependency = Annotated[DraftRca, Depends(draft_rca)]
