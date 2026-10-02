from fastapi import APIRouter

from incident_insights.application.outputs.anomalies import AnomalyReportOutput
from incident_insights.interface.api.dependencies import DetectAnomaliesDependency
from incident_insights.interface.api.routers.params import Days

router = APIRouter(prefix="/api", tags=["anomalies"])


@router.get("/anomalies")
def get_anomalies(use_case: DetectAnomaliesDependency, days: Days = 180) -> AnomalyReportOutput:
    return use_case.execute(days)
