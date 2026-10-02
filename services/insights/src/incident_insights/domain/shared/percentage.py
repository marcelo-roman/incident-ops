from dataclasses import dataclass

DIGITS = 1
HUNDRED = 100.0


@dataclass(frozen=True, slots=True)
class Percentage:
    value: float

    def __post_init__(self) -> None:
        if not 0.0 <= self.value <= HUNDRED:
            raise ValueError(f"{self.value} is not a percentage")

    @classmethod
    def from_fraction(cls, fraction: float) -> "Percentage":
        return cls(round(HUNDRED * fraction, DIGITS))

    @classmethod
    def share(cls, part: int, total: int) -> "Percentage | None":
        if total == 0:
            return None
        return cls(round(HUNDRED * part / total, DIGITS))
