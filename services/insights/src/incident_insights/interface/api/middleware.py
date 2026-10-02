import logging
import time
from collections.abc import Awaitable, Callable
from uuid import uuid4

from fastapi import Request, Response

from incident_insights.infrastructure.observability.correlation import correlation_id

CORRELATION_HEADER = "X-Correlation-Id"
MAX_CORRELATION_LENGTH = 128

logger = logging.getLogger("incident_insights.http")


async def correlation_middleware(
    request: Request, call_next: Callable[[Request], Awaitable[Response]]
) -> Response:
    value = _correlation_value(request)
    token = correlation_id.set(value)
    started = time.perf_counter()
    try:
        response = await call_next(request)
        response.headers[CORRELATION_HEADER] = value
        logger.info(
            "request completed",
            extra={
                "method": request.method,
                "path": request.url.path,
                "status": response.status_code,
                "durationMs": round((time.perf_counter() - started) * 1000, 1),
            },
        )
        return response
    finally:
        correlation_id.reset(token)


def _correlation_value(request: Request) -> str:
    supplied = request.headers.get(CORRELATION_HEADER, "")
    if supplied and len(supplied) <= MAX_CORRELATION_LENGTH and supplied.isprintable():
        return supplied
    return uuid4().hex
