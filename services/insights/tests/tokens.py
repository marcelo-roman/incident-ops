from datetime import UTC, datetime, timedelta
from typing import Any

import jwt
from fastapi.testclient import TestClient
from pydantic import SecretStr

SIGNING_KEY = "insights-test-signing-key-0123456789abcdef"
OTHER_KEY = "another-signing-key-0123456789abcdef-xyz"
ISSUER = "incident-ops-api"
AUDIENCE = "incident-ops"


def signing_key() -> SecretStr:
    return SecretStr(SIGNING_KEY)


def issue_token(
    key: str = SIGNING_KEY,
    lifetime: timedelta = timedelta(hours=8),
    **overrides: Any,
) -> str:
    now = datetime.now(UTC)
    claims: dict[str, Any] = {
        "sub": "demo",
        "name": "demo",
        "iss": ISSUER,
        "aud": AUDIENCE,
        "iat": now,
        "nbf": now,
        "exp": now + lifetime,
    }
    return jwt.encode({**claims, **overrides}, key, algorithm="HS256")


def authorized(client: TestClient) -> TestClient:
    client.headers["Authorization"] = f"Bearer {issue_token()}"
    return client
