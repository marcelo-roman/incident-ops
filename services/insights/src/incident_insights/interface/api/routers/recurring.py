from fastapi import APIRouter

from incident_insights.application.outputs.recurring import RecurringReportOutput
from incident_insights.interface.api.dependencies import FindRecurringDependency
from incident_insights.interface.api.routers.params import Days

router = APIRouter(prefix="/api", tags=["recurring"])


@router.get("/recurring")
def get_recurring(use_case: FindRecurringDependency, days: Days = 180) -> RecurringReportOutput:
    return use_case.execute(days)
