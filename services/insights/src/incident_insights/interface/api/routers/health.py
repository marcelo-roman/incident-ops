from fastapi import APIRouter

from incident_insights.interface.api.schemas import HealthStatus

router = APIRouter(tags=["health"])


@router.get("/health")
def health() -> HealthStatus:
    return HealthStatus(status="ok")
