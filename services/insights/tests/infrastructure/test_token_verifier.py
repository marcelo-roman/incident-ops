from datetime import timedelta

import pytest

from incident_insights.infrastructure.security.factory import build_token_verifier
from incident_insights.infrastructure.security.token_verifier import (
    Caller,
    InvalidTokenError,
    JwtTokenVerifier,
    SigningKeyError,
)
from incident_insights.infrastructure.settings import Settings
from tests.tokens import OTHER_KEY, SIGNING_KEY, issue_token

VERIFIER = JwtTokenVerifier(SIGNING_KEY)


def test_valid_token_yields_the_caller() -> None:
    assert VERIFIER.verify(issue_token(name="recruiter")) == Caller(name="recruiter")


def test_subject_is_used_when_name_is_missing() -> None:
    assert VERIFIER.verify(issue_token(sub="demo", name=None)) == Caller(name="demo")


def test_token_within_clock_skew_is_accepted() -> None:
    assert VERIFIER.verify(issue_token(lifetime=timedelta(seconds=-10))).name == "demo"


@pytest.mark.parametrize(
    "token",
    [
        issue_token(key=OTHER_KEY),
        issue_token(lifetime=timedelta(minutes=-5)),
        issue_token(iss="someone-else"),
        issue_token(aud="another-audience"),
        issue_token(exp=None),
        issue_token(sub=None),
        "not-a-jwt",
    ],
    ids=[
        "signature",
        "expired",
        "issuer",
        "audience",
        "missing-exp",
        "missing-sub",
        "malformed",
    ],
)
def test_invalid_tokens_raise(token: str) -> None:
    with pytest.raises(InvalidTokenError):
        VERIFIER.verify(token)


def test_tokens_signed_with_another_algorithm_are_rejected() -> None:
    unsigned = issue_token().rsplit(".", 1)[0].replace("HS256", "none")

    with pytest.raises(InvalidTokenError):
        VERIFIER.verify(f"{unsigned}.")


def test_short_signing_key_is_rejected() -> None:
    with pytest.raises(SigningKeyError):
        JwtTokenVerifier("short")


def test_factory_reads_the_signing_key_from_settings() -> None:
    verifier = build_token_verifier(Settings(auth_signing_key=SIGNING_KEY))

    assert verifier.verify(issue_token()).name == "demo"


def test_factory_rejects_missing_signing_key() -> None:
    with pytest.raises(SigningKeyError):
        build_token_verifier(Settings())
