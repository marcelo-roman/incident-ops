from typing import Annotated

from fastapi import Depends, Request
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer

from incident_insights.infrastructure.security.token_verifier import (
    Caller,
    InvalidTokenError,
    JwtTokenVerifier,
)

_bearer = HTTPBearer(
    auto_error=False,
    bearerFormat="JWT",
    description="Access token issued by POST /api/auth/token on the Incidents API.",
)


class NotAuthenticatedError(Exception):
    pass


def authenticated_caller(
    request: Request,
    credentials: Annotated[HTTPAuthorizationCredentials | None, Depends(_bearer)],
) -> Caller:
    if credentials is None:
        raise NotAuthenticatedError("A bearer token is required.")
    verifier: JwtTokenVerifier = request.app.state.token_verifier
    try:
        return verifier.verify(credentials.credentials)
    except InvalidTokenError as error:
        raise NotAuthenticatedError("The bearer token is invalid or expired.") from error


AuthenticatedCaller = Depends(authenticated_caller)
