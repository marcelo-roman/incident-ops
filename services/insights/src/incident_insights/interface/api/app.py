from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from incident_insights.application.use_cases import UseCases
from incident_insights.infrastructure.composition import build_use_cases
from incident_insights.infrastructure.observability.logging import configure_logging
from incident_insights.infrastructure.observability.telemetry import configure_telemetry
from incident_insights.infrastructure.security.factory import build_token_verifier
from incident_insights.infrastructure.settings import Settings
from incident_insights.interface.api.authentication import AuthenticatedCaller
from incident_insights.interface.api.middleware import CORRELATION_HEADER, correlation_middleware
from incident_insights.interface.api.problems import register_problem_handlers
from incident_insights.interface.api.routers import anomalies, health, kpis, rca, recurring

PUBLIC_ROUTERS = (health.router,)
PROTECTED_ROUTERS = (kpis.router, recurring.router, anomalies.router, rca.router)


def create_app(settings: Settings | None = None, use_cases: UseCases | None = None) -> FastAPI:
    resolved = settings or Settings()
    token_verifier = build_token_verifier(resolved)
    configure_logging(resolved.log_level)
    configure_telemetry(resolved)
    app = FastAPI(title="Incident Ops Insights", version="0.1.0")
    app.state.token_verifier = token_verifier
    app.state.use_cases = use_cases or build_use_cases(resolved)
    app.middleware("http")(correlation_middleware)
    app.add_middleware(
        CORSMiddleware,
        allow_origins=resolved.cors_allowed_origins,
        allow_methods=["GET", "POST"],
        allow_headers=["Authorization", "Content-Type", CORRELATION_HEADER],
        expose_headers=[CORRELATION_HEADER],
    )
    register_problem_handlers(app)
    for router in PUBLIC_ROUTERS:
        app.include_router(router)
    for router in PROTECTED_ROUTERS:
        app.include_router(router, dependencies=[AuthenticatedCaller])
    return app
