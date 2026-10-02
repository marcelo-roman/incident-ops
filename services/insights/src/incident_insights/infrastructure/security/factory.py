from incident_insights.infrastructure.security.token_verifier import JwtTokenVerifier
from incident_insights.infrastructure.settings import Settings


def build_token_verifier(settings: Settings) -> JwtTokenVerifier:
    return JwtTokenVerifier(_signing_key(settings))


def _signing_key(settings: Settings) -> str:
    if settings.auth_signing_key is None:
        return ""
    return settings.auth_signing_key.get_secret_value()
