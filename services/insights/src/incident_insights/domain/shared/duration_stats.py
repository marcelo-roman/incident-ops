from collections.abc import Sequence
from dataclasses import dataclass

import numpy as np

DIGITS = 1
P90 = 0.9


@dataclass(frozen=True, slots=True)
class DurationStats:
    samples: int
    median_minutes: float | None
    p90_minutes: float | None

    def __post_init__(self) -> None:
        if self.samples < 0:
            raise ValueError("samples cannot be negative")
        if (self.samples == 0) != (self.median_minutes is None):
            raise ValueError("a median exists exactly when there are samples")

    @classmethod
    def of_minutes(cls, minutes: Sequence[float]) -> "DurationStats":
        if not minutes:
            return cls(samples=0, median_minutes=None, p90_minutes=None)
        values = np.asarray(minutes, dtype=float)
        return cls(
            samples=len(values),
            median_minutes=round(float(np.median(values)), DIGITS),
            p90_minutes=round(float(np.quantile(values, P90)), DIGITS),
        )
