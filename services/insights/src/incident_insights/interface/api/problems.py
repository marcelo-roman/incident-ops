import logging
from http import HTTPStatus
from typing import Any

from fastapi import FastAPI, Request
from fastapi.encoders import jsonable_encoder
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

from incident_insights.application.draft_rca import IncidentNotFoundError
from incident_insights.application.ports import IncidentSourceError, RcaDraftError
from incident_insights.interface.api.authentication import NotAuthenticatedError

PROBLEM_MEDIA_TYPE = "application/problem+json"

logger = logging.getLogger(__name__)


def problem(
    status: HTTPStatus,
    detail: str,
    headers: dict[str, str] | None = None,
    **extensions: Any,
) -> JSONResponse:
    body = {"type": "about:blank", "title": status.phrase, "status": status.value, "detail": detail}
    return JSONResponse(
        jsonable_encoder({**body, **extensions}),
        status_code=status.value,
        media_type=PROBLEM_MEDIA_TYPE,
        headers=headers,
    )


async def _unauthorized(request: Request, error: Exception) -> JSONResponse:
    return problem(
        HTTPStatus.UNAUTHORIZED,
        str(error),
        headers={"WWW-Authenticate": "Bearer"},
        instance=request.url.path,
    )


async def _not_found(request: Request, error: Exception) -> JSONResponse:
    return problem(HTTPStatus.NOT_FOUND, str(error), instance=request.url.path)


async def _bad_gateway(request: Request, error: Exception) -> JSONResponse:
    logger.warning("upstream failure", extra={"path": request.url.path, "error": str(error)})
    return problem(HTTPStatus.BAD_GATEWAY, str(error), instance=request.url.path)


async def _invalid_request(request: Request, error: Exception) -> JSONResponse:
    return problem(
        HTTPStatus.UNPROCESSABLE_ENTITY,
        "The request is invalid.",
        instance=request.url.path,
        errors=_validation_errors(error),
    )


def _validation_errors(error: Exception) -> list[Any]:
    if not isinstance(error, RequestValidationError):
        return []
    return list(error.errors())


def register_problem_handlers(app: FastAPI) -> None:
    app.add_exception_handler(NotAuthenticatedError, _unauthorized)
    app.add_exception_handler(IncidentNotFoundError, _not_found)
    app.add_exception_handler(IncidentSourceError, _bad_gateway)
    app.add_exception_handler(RcaDraftError, _bad_gateway)
    app.add_exception_handler(RequestValidationError, _invalid_request)
