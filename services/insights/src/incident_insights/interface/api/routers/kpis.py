from fastapi import APIRouter

from incident_insights.application.outputs.kpis import KpiReportOutput
from incident_insights.interface.api.dependencies import GetKpisDependency
from incident_insights.interface.api.routers.params import Days

router = APIRouter(prefix="/api", tags=["kpis"])


@router.get("/kpis")
def get_kpis(use_case: GetKpisDependency, days: Days = 90) -> KpiReportOutput:
    return use_case.execute(days)
