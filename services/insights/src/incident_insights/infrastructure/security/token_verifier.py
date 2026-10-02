from dataclasses import dataclass

import jwt

ISSUER = "incident-ops-api"
AUDIENCE = "incident-ops"
SIGNING_ALGORITHM = "HS256"
MIN_SIGNING_KEY_BYTES = 32
CLOCK_SKEW_SECONDS = 30
REQUIRED_CLAIMS = ("exp", "iss", "aud", "sub")


class SigningKeyError(ValueError):
    pass


class InvalidTokenError(Exception):
    pass


@dataclass(frozen=True)
class Caller:
    name: str


class JwtTokenVerifier:
    def __init__(self, signing_key: str) -> None:
        if len(signing_key.encode()) < MIN_SIGNING_KEY_BYTES:
            raise SigningKeyError(
                f"AUTH_SIGNING_KEY must be at least {MIN_SIGNING_KEY_BYTES} bytes long."
            )
        self._signing_key = signing_key

    def verify(self, token: str) -> Caller:
        try:
            claims = jwt.decode(
                token,
                self._signing_key,
                algorithms=[SIGNING_ALGORITHM],
                issuer=ISSUER,
                audience=AUDIENCE,
                leeway=CLOCK_SKEW_SECONDS,
                options={"require": list(REQUIRED_CLAIMS)},
            )
        except jwt.PyJWTError as error:
            raise InvalidTokenError(str(error)) from error
        return Caller(name=str(claims.get("name") or claims["sub"]))
