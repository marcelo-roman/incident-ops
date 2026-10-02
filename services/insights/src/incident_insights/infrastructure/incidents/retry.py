import logging
import random
import time
from collections.abc import Callable
from dataclasses import dataclass

import httpx

logger = logging.getLogger(__name__)


@dataclass(frozen=True)
class RetryPolicy:
    attempts: int = 3
    base_delay_seconds: float = 0.2
    max_delay_seconds: float = 2.0

    def ceiling(self, attempt: int) -> float:
        return min(self.max_delay_seconds, self.base_delay_seconds * 2.0 ** (attempt - 1))


def full_jitter(ceiling: float) -> float:
    return random.uniform(0.0, ceiling)


class RetryingGetter:
    def __init__(
        self,
        client: httpx.Client,
        policy: RetryPolicy,
        sleep: Callable[[float], None] = time.sleep,
        jitter: Callable[[float], float] = full_jitter,
    ) -> None:
        self._client = client
        self._policy = policy
        self._sleep = sleep
        self._jitter = jitter

    def get(self, path: str, params: dict[str, str] | None) -> httpx.Response:
        for attempt in range(1, self._policy.attempts):
            response = self._attempt(path, params)
            if response is not None and not response.is_server_error:
                return response
            self._back_off(path, attempt, response)
        return self._client.get(path, params=params)

    def _attempt(self, path: str, params: dict[str, str] | None) -> httpx.Response | None:
        try:
            return self._client.get(path, params=params)
        except httpx.TransportError as error:
            logger.warning("upstream transport error", extra={"path": path, "error": str(error)})
            return None

    def _back_off(self, path: str, attempt: int, response: httpx.Response | None) -> None:
        delay = self._jitter(self._policy.ceiling(attempt))
        status = None
        if response is not None:
            status = response.status_code
        logger.warning(
            "retrying upstream request",
            extra={"path": path, "attempt": attempt, "status": status, "delaySeconds": delay},
        )
        self._sleep(delay)
