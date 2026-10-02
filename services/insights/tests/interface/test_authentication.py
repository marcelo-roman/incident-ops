from datetime import timedelta

import pytest
from fastapi.testclient import TestClient

from incident_insights.application.clock import fixed_clock
from incident_insights.infrastructure.composition import build_use_cases
from incident_insights.infrastructure.security.token_verifier import SigningKeyError
from incident_insights.infrastructure.settings import DataSourceKind, Settings
from incident_insights.interface.api.app import create_app
from tests.builders import AS_OF
from tests.tokens import OTHER_KEY, issue_token, signing_key

PROTECTED = (
    ("GET", "/api/kpis"),
    ("GET", "/api/recurring"),
    ("GET", "/api/anomalies"),
    ("POST", "/api/rca/draft"),
)


@pytest.fixture
def client(sample_csv: object) -> TestClient:
    settings = Settings(
        insights_data_source=DataSourceKind.CSV,
        insights_csv_path=sample_csv,
        auth_signing_key=signing_key(),
    )
    return TestClient(create_app(settings, build_use_cases(settings, fixed_clock(AS_OF))))


def _bearer(token: str) -> dict[str, str]:
    return {"Authorization": f"Bearer {token}"}


@pytest.mark.parametrize(("method", "path"), PROTECTED)
def test_api_endpoints_require_a_token(client: TestClient, method: str, path: str) -> None:
    response = client.request(method, path, json={})

    assert response.status_code == 401
    assert response.headers["www-authenticate"] == "Bearer"
    assert response.headers["content-type"] == "application/problem+json"
    assert response.json()["detail"] == "A bearer token is required."


def test_valid_token_is_accepted(client: TestClient) -> None:
    response = client.get("/api/kpis", params={"days": 30}, headers=_bearer(issue_token()))

    assert response.status_code == 200


def test_token_without_name_claim_is_accepted(client: TestClient) -> None:
    token = issue_token(name=None)

    assert client.get("/api/anomalies", headers=_bearer(token)).status_code == 200


@pytest.mark.parametrize(
    "token",
    [
        issue_token(key=OTHER_KEY),
        issue_token(lifetime=timedelta(minutes=-5)),
        issue_token(iss="someone-else"),
        issue_token(aud="another-audience"),
        "not-a-jwt",
    ],
    ids=["wrong-signature", "expired", "wrong-issuer", "wrong-audience", "malformed"],
)
def test_invalid_tokens_are_rejected(client: TestClient, token: str) -> None:
    response = client.get("/api/kpis", headers=_bearer(token))

    assert response.status_code == 401
    assert response.headers["www-authenticate"] == "Bearer"
    assert response.json()["detail"] == "The bearer token is invalid or expired."


def test_non_bearer_scheme_is_rejected(client: TestClient) -> None:
    response = client.get("/api/kpis", headers={"Authorization": "Basic ZGVtbzpkZW1v"})

    assert response.status_code == 401


@pytest.mark.parametrize("path", ["/health", "/docs", "/openapi.json"])
def test_health_and_docs_are_anonymous(client: TestClient, path: str) -> None:
    assert client.get(path).status_code == 200


def test_openapi_declares_bearer_scheme_on_api_only(client: TestClient) -> None:
    document = client.get("/openapi.json").json()

    scheme = document["components"]["securitySchemes"]["HTTPBearer"]
    assert scheme == {
        "type": "http",
        "scheme": "bearer",
        "bearerFormat": "JWT",
        "description": "Access token issued by POST /api/auth/token on the Incidents API.",
    }
    assert document["paths"]["/api/kpis"]["get"]["security"] == [{"HTTPBearer": []}]
    assert "security" not in document["paths"]["/health"]["get"]


@pytest.mark.parametrize("key", [None, "", "too-short-signing-key"])
def test_app_refuses_to_start_without_a_strong_signing_key(key: str | None) -> None:
    settings = Settings.model_validate({"auth_signing_key": key})

    with pytest.raises(SigningKeyError, match="AUTH_SIGNING_KEY"):
        create_app(settings)
