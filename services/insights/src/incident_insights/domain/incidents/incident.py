from dataclasses import dataclass, field
from datetime import date, datetime, timedelta
from uuid import UUID, uuid5

from incident_insights.domain.incidents.business_hours import ON_CALL_BUSINESS_HOURS
from incident_insights.domain.incidents.sla import (
    STANDARD_SLA_POLICY,
    SlaOutcome,
    SlaState,
    SlaTargets,
)
from incident_insights.domain.incidents.status import IncidentStatus
from incident_insights.domain.incidents.timeline import TimelineEntry, TimelineKind
from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.reporting_window import week_start_of
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.domain.shared.severity import Severity

SYSTEM_ACTOR = "system"
UNASSIGNED = "unassigned"
SECONDS_PER_MINUTE = 60


@dataclass(frozen=True, slots=True, eq=False)
class IncidentRecord:
    id: UUID
    number: int
    title: str
    description: str
    service: ServiceId
    severity: Severity
    status: IncidentStatus
    created_at: datetime
    acknowledge_due_at: datetime
    resolve_due_at: datetime
    sla_state: SlaState
    source: DetectionSource = DetectionSource.MANUAL
    assignee: str | None = None
    escalation_level: int = 1
    acknowledged_at: datetime | None = None
    mitigated_at: datetime | None = None
    resolved_at: datetime | None = None
    acknowledgement_breached: bool = False
    root_cause: str | None = None
    alert_fingerprint: str | None = None
    timeline: tuple[TimelineEntry, ...] = field(default=())

    def __post_init__(self) -> None:
        if self.created_at.tzinfo is None:
            raise ValueError("incident timestamps must be timezone-aware")
        if self.escalation_level < 1:
            raise ValueError("escalation starts at level 1")
        if self.resolved_at is not None and self.resolved_at < self.created_at:
            raise ValueError("an incident cannot be resolved before it was created")
        if self.acknowledged_at is not None and self.acknowledged_at < self.created_at:
            raise ValueError("an incident cannot be acknowledged before it was created")

    def __eq__(self, other: object) -> bool:
        if not isinstance(other, IncidentRecord):
            return NotImplemented
        return self.id == other.id

    def __hash__(self) -> int:
        return hash(self.id)

    @property
    def targets(self) -> SlaTargets:
        return STANDARD_SLA_POLICY.targets_for(self.severity)

    @property
    def is_escalated(self) -> bool:
        return self.escalation_level > 1

    @property
    def responder(self) -> str:
        return self.assignee or UNASSIGNED

    @property
    def is_resolved(self) -> bool:
        return self.resolved_at is not None

    def time_to_acknowledge(self) -> timedelta | None:
        return _elapsed(self.created_at, self.acknowledged_at)

    def time_to_mitigate(self) -> timedelta | None:
        return _elapsed(self.created_at, self.mitigated_at)

    def time_to_resolve(self) -> timedelta | None:
        return _elapsed(self.created_at, self.resolved_at)

    def minutes_to_acknowledge(self) -> float | None:
        return _minutes(self.time_to_acknowledge())

    def minutes_to_resolve(self) -> float | None:
        return _minutes(self.time_to_resolve())

    def acknowledgement_outcome(self) -> SlaOutcome:
        acknowledged = self.acknowledged_at is not None
        breached = self.acknowledgement_breached or (not acknowledged and self.is_resolved)
        return SlaOutcome.judge(breached=breached, met=acknowledged and not breached)

    def resolution_outcome(self, as_of: datetime) -> SlaOutcome:
        breached = (self.resolved_at or as_of) > self.resolve_due_at
        return SlaOutcome.judge(breached=breached, met=self.is_resolved and not breached)

    def sla_outcome(self, as_of: datetime) -> SlaOutcome:
        acknowledgement = self.acknowledgement_outcome()
        resolution = self.resolution_outcome(as_of)
        breached = SlaOutcome.BREACHED in (acknowledgement, resolution)
        met = acknowledgement is SlaOutcome.MET and resolution is SlaOutcome.MET
        return SlaOutcome.judge(breached=breached, met=met)

    def complies_with_sla(self) -> bool:
        if self.acknowledged_at is None or self.acknowledgement_breached:
            return False
        return self.resolved_at is not None and self.resolved_at <= self.resolve_due_at

    def is_settled(self, as_of: datetime) -> bool:
        return self.sla_outcome(as_of).is_decided

    def resolution_overrun(self, as_of: datetime) -> timedelta:
        return (self.resolved_at or as_of) - self.resolve_due_at

    def resolved_later_than_policy(self) -> bool:
        elapsed = self.time_to_resolve()
        return elapsed is not None and elapsed > self.targets.resolve_within

    def detected_by_monitoring(self) -> bool:
        return self.source.is_monitoring

    def created_off_hours(self) -> bool:
        return ON_CALL_BUSINESS_HOURS.excludes(self.created_at)

    def week_start(self) -> date:
        return week_start_of(self.created_at)

    def chronology(self) -> tuple[TimelineEntry, ...]:
        entries = self.timeline or self._lifecycle_timeline()
        return tuple(sorted(entries, key=lambda entry: entry.at))

    def _lifecycle_timeline(self) -> tuple[TimelineEntry, ...]:
        responder = self.assignee or SYSTEM_ACTOR
        milestones = (
            (TimelineKind.TRIGGERED, self.created_at, self.source.value, self.title),
            (TimelineKind.ACKNOWLEDGED, self.acknowledged_at, responder, "Page acknowledged"),
            (TimelineKind.MITIGATED, self.mitigated_at, responder, "Impact mitigated"),
            (TimelineKind.RESOLVED, self.resolved_at, responder, self._resolution_note()),
        )
        return tuple(
            TimelineEntry(uuid5(self.id, kind.value), at, kind, actor, message)
            for kind, at, actor, message in milestones
            if at is not None
        )

    def _resolution_note(self) -> str:
        if self.root_cause is None:
            return "Incident resolved"
        return f"Root cause: {self.root_cause}"


def _elapsed(start: datetime, end: datetime | None) -> timedelta | None:
    if end is None:
        return None
    return end - start


def _minutes(duration: timedelta | None) -> float | None:
    if duration is None:
        return None
    return duration.total_seconds() / SECONDS_PER_MINUTE
