from dataclasses import dataclass
from datetime import datetime
from enum import StrEnum
from uuid import UUID


class TimelineKind(StrEnum):
    TRIGGERED = "Triggered"
    ACKNOWLEDGED = "Acknowledged"
    ESCALATED = "Escalated"
    MITIGATED = "Mitigated"
    RESOLVED = "Resolved"
    NOTE = "Note"
    ALERT = "Alert"


@dataclass(frozen=True, slots=True)
class TimelineEntry:
    id: UUID
    at: datetime
    kind: TimelineKind
    actor: str
    message: str

    def __post_init__(self) -> None:
        if self.at.tzinfo is None:
            raise ValueError("timeline entries need a timezone-aware timestamp")
