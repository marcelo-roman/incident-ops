from dataclasses import dataclass
from enum import StrEnum

from incident_insights.domain.shared.severity import Severity


class InvalidRcaDraftError(ValueError):
    pass


class Priority(StrEnum):
    P1 = "P1"
    P2 = "P2"
    P3 = "P3"

    @classmethod
    def for_severity(cls, severity: Severity) -> "Priority":
        if severity.is_high:
            return cls.P1
        if severity is Severity.SEV3:
            return cls.P2
        return cls.P3


@dataclass(frozen=True, slots=True)
class ActionItem:
    title: str
    owner: str
    priority: Priority

    def __post_init__(self) -> None:
        if not self.title.strip():
            raise InvalidRcaDraftError("an action item needs a title")
        if not self.owner.strip():
            raise InvalidRcaDraftError("an action item needs an owner")
        if not isinstance(self.priority, Priority):
            raise InvalidRcaDraftError(f"'{self.priority}' is not a valid priority")


@dataclass(frozen=True, slots=True)
class RcaTimelineEvent:
    at: str
    event: str

    def __post_init__(self) -> None:
        if not self.event.strip():
            raise InvalidRcaDraftError("a timeline event needs a description")


@dataclass(frozen=True, slots=True)
class RcaDraft:
    summary: str
    impact: str
    timeline: tuple[RcaTimelineEvent, ...]
    contributing_factors: tuple[str, ...]
    action_items: tuple[ActionItem, ...]

    def __post_init__(self) -> None:
        if not self.summary.strip():
            raise InvalidRcaDraftError("an RCA draft needs a summary")
        if not self.impact.strip():
            raise InvalidRcaDraftError("an RCA draft needs an impact statement")
        if any(not factor.strip() for factor in self.contributing_factors):
            raise InvalidRcaDraftError("contributing factors cannot be blank")
