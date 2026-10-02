import re
from dataclasses import dataclass

SLUG = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")


@dataclass(frozen=True, order=True, slots=True)
class ServiceId:
    value: str

    def __post_init__(self) -> None:
        if not SLUG.fullmatch(self.value):
            raise ValueError(f"'{self.value}' is not a service slug")

    def __str__(self) -> str:
        return self.value
