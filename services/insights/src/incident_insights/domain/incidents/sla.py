from collections.abc import Mapping
from dataclasses import dataclass
from datetime import datetime, timedelta
from enum import StrEnum

from incident_insights.domain.shared.severity import Severity

AT_RISK_REMAINING_FRACTION = 0.25


class SlaOutcome(StrEnum):
    MET = "Met"
    BREACHED = "Breached"
    PENDING = "Pending"

    @property
    def is_decided(self) -> bool:
        return self is not SlaOutcome.PENDING

    @classmethod
    def judge(cls, *, breached: bool, met: bool) -> "SlaOutcome":
        if breached:
            return cls.BREACHED
        if met:
            return cls.MET
        return cls.PENDING


@dataclass(frozen=True, slots=True)
class SlaTargets:
    acknowledge_within: timedelta
    resolve_within: timedelta

    def __post_init__(self) -> None:
        if self.acknowledge_within <= timedelta(0) or self.resolve_within <= timedelta(0):
            raise ValueError("SLA targets must be positive")
        if self.acknowledge_within > self.resolve_within:
            raise ValueError("acknowledgement target cannot exceed the resolution target")


@dataclass(frozen=True, slots=True)
class SlaPolicy:
    targets: Mapping[Severity, SlaTargets]

    def __post_init__(self) -> None:
        missing = set(Severity) - set(self.targets)
        if missing:
            raise ValueError(f"SLA policy lacks targets for {sorted(missing)}")

    def targets_for(self, severity: Severity) -> SlaTargets:
        return self.targets[severity]


STANDARD_SLA_POLICY = SlaPolicy(
    {
        Severity.SEV1: SlaTargets(timedelta(minutes=15), timedelta(hours=4)),
        Severity.SEV2: SlaTargets(timedelta(minutes=30), timedelta(hours=8)),
        Severity.SEV3: SlaTargets(timedelta(hours=4), timedelta(days=3)),
        Severity.SEV4: SlaTargets(timedelta(hours=24), timedelta(days=10)),
    }
)


class SlaState(StrEnum):
    ON_TRACK = "OnTrack"
    AT_RISK = "AtRisk"
    BREACHED = "Breached"
    MET = "Met"

    @classmethod
    def evaluate(
        cls,
        *,
        created_at: datetime,
        acknowledged_at: datetime | None,
        acknowledgement_breached: bool,
        resolved_at: datetime | None,
        acknowledge_due_at: datetime,
        resolve_due_at: datetime,
        as_of: datetime,
    ) -> "SlaState":
        if resolved_at is not None:
            return cls._settled(
                acknowledged_at is not None
                and not acknowledgement_breached
                and resolved_at <= resolve_due_at
            )
        if acknowledged_at is None and as_of > acknowledge_due_at:
            return cls.BREACHED
        if as_of > resolve_due_at:
            return cls.BREACHED
        deadline = resolve_due_at
        if acknowledged_at is None:
            deadline = acknowledge_due_at
        remaining = (deadline - as_of) / (deadline - created_at)
        if remaining < AT_RISK_REMAINING_FRACTION:
            return cls.AT_RISK
        return cls.ON_TRACK

    @classmethod
    def _settled(cls, compliant: bool) -> "SlaState":
        if compliant:
            return cls.MET
        return cls.BREACHED
