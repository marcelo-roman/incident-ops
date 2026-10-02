from fastapi import APIRouter

from incident_insights.application.outputs.rca import RcaDraftOutput
from incident_insights.interface.api.dependencies import DraftRcaDependency
from incident_insights.interface.api.schemas import RcaDraftRequest

router = APIRouter(prefix="/api/rca", tags=["rca"])


@router.post("/draft")
def draft_rca(request: RcaDraftRequest, use_case: DraftRcaDependency) -> RcaDraftOutput:
    return use_case.execute(request.incident_id)
