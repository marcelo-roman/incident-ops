from collections.abc import Iterator
from dataclasses import dataclass
from datetime import UTC, datetime, timedelta
from uuid import UUID
from zoneinfo import ZoneInfo

import numpy as np

from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.incidents.sla import STANDARD_SLA_POLICY, SlaState, SlaTargets
from incident_insights.domain.incidents.status import IncidentStatus
from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.domain.shared.severity import Severity
from incident_insights.infrastructure.sample.catalog import (
    ALERTMANAGER_SHARE,
    ENGINEERS,
    REGIONS,
    SERVICES,
    SHARED_FAMILIES,
    SHARED_FAMILY_SHARE,
    VOLUME_SPIKES,
    IssueFamily,
    SampleService,
)

DEFAULT_END = datetime(2026, 9, 28, tzinfo=UTC)
DEFAULT_DAYS = 182
DEFAULT_SEED = 42
FIRST_NUMBER = 1001
MINUTES_PER_DAY = 1440
ROTATION_ANCHOR = datetime(2026, 1, 5, 10, 30, tzinfo=ZoneInfo("America/New_York"))
SECONDARY_SHARE = 0.15
MITIGATION_SHARE = 0.7
MITIGATION_POINT = 0.6
ALERTED_ACK_MEDIAN_FRACTION = 0.3
MANUAL_ACK_MEDIAN_FRACTION = 0.55
FINGERPRINT_BYTES = 8
RESOLVE_MEDIAN_FRACTION = 0.35
DURATION_SIGMA = 0.9
SEVERITIES = (Severity.SEV1, Severity.SEV2, Severity.SEV3, Severity.SEV4)
MAX_ESCALATION_LEVEL = 3


@dataclass(frozen=True)
class SampleSpec:
    end: datetime = DEFAULT_END
    days: int = DEFAULT_DAYS
    seed: int = DEFAULT_SEED


@dataclass(frozen=True)
class Occurrence:
    created_at: datetime
    service: SampleService
    family: IssueFamily


@dataclass(frozen=True)
class Lifecycle:
    acknowledged_at: datetime | None
    mitigated_at: datetime | None
    resolved_at: datetime | None
    escalation_level: int
    ack_due_at: datetime
    acknowledgement_breached: bool


def generate_incidents(spec: SampleSpec) -> list[IncidentRecord]:
    rng = np.random.default_rng(spec.seed)
    occurrences = sorted(_occurrences(rng, spec), key=lambda item: item.created_at)
    return [
        _incident(rng, occurrence, number, spec.end)
        for number, occurrence in enumerate(occurrences, start=FIRST_NUMBER)
    ]


def _occurrences(rng: np.random.Generator, spec: SampleSpec) -> Iterator[Occurrence]:
    first_day = spec.end.replace(hour=0, minute=0, second=0, microsecond=0) - timedelta(
        days=spec.days
    )
    for offset in range(spec.days):
        day = first_day + timedelta(days=offset)
        for service in SERVICES:
            rate = service.daily_rate * _spike_multiplier(service.id, day, spec.end)
            for _ in range(int(rng.poisson(rate))):
                minute = int(rng.integers(0, MINUTES_PER_DAY))
                created_at = day + timedelta(minutes=minute)
                yield Occurrence(created_at, service, _family(rng, service))


def _spike_multiplier(service_id: str, day: datetime, end: datetime) -> float:
    weeks_back = (end - day).days // 7
    for spike in VOLUME_SPIKES:
        if spike.service_id == service_id and spike.week_offset == weeks_back:
            return spike.multiplier
    return 1.0


def _family(rng: np.random.Generator, service: SampleService) -> IssueFamily:
    if rng.random() < SHARED_FAMILY_SHARE:
        return SHARED_FAMILIES[int(rng.integers(0, len(SHARED_FAMILIES)))]
    return service.families[int(rng.integers(0, len(service.families)))]


def _incident(
    rng: np.random.Generator, occurrence: Occurrence, number: int, as_of: datetime
) -> IncidentRecord:
    family = occurrence.family
    source = _source(rng, family)
    severity = SEVERITIES[int(rng.choice(len(SEVERITIES), p=family.severity_weights))]
    target = STANDARD_SLA_POLICY.targets_for(severity)
    created_at = occurrence.created_at
    lifecycle = _lifecycle(rng, created_at, target, source, as_of)
    title, description = _text(rng, family, occurrence.service.id)
    resolve_due_at = created_at + target.resolve_within
    incident_id = UUID(bytes=rng.bytes(16), version=4)
    assignee = _assignee(rng, created_at, lifecycle)
    fingerprint = _fingerprint(rng, source)
    return IncidentRecord(
        id=incident_id,
        number=number,
        title=title,
        description=description,
        service=ServiceId(occurrence.service.id),
        severity=severity,
        status=_status(lifecycle),
        created_at=created_at,
        acknowledge_due_at=lifecycle.ack_due_at,
        resolve_due_at=resolve_due_at,
        sla_state=SlaState.evaluate(
            created_at=created_at,
            acknowledged_at=lifecycle.acknowledged_at,
            acknowledgement_breached=lifecycle.acknowledgement_breached,
            resolved_at=lifecycle.resolved_at,
            acknowledge_due_at=lifecycle.ack_due_at,
            resolve_due_at=resolve_due_at,
            as_of=as_of,
        ),
        source=source,
        assignee=assignee,
        escalation_level=lifecycle.escalation_level,
        acknowledged_at=lifecycle.acknowledged_at,
        mitigated_at=lifecycle.mitigated_at,
        resolved_at=lifecycle.resolved_at,
        acknowledgement_breached=lifecycle.acknowledgement_breached,
        root_cause=_root_cause(family, lifecycle),
        alert_fingerprint=fingerprint,
    )


def _source(rng: np.random.Generator, family: IssueFamily) -> DetectionSource:
    if rng.random() >= family.alert_share:
        return DetectionSource.MANUAL
    if rng.random() < ALERTMANAGER_SHARE:
        return DetectionSource.ALERTMANAGER
    return DetectionSource.AZURE_MONITOR


def _fingerprint(rng: np.random.Generator, source: DetectionSource) -> str | None:
    if not source.is_monitoring:
        return None
    return rng.bytes(FINGERPRINT_BYTES).hex()


def _lifecycle(
    rng: np.random.Generator,
    created_at: datetime,
    target: SlaTargets,
    source: DetectionSource,
    as_of: datetime,
) -> Lifecycle:
    ack_after = _duration(rng, target.acknowledge_within, _ack_median_fraction(source))
    resolve_after = ack_after + _duration(rng, target.resolve_within, RESOLVE_MEDIAN_FRACTION)
    mitigate_after = ack_after + timedelta(
        seconds=round((resolve_after - ack_after).total_seconds() * MITIGATION_POINT)
    )
    waited = min(ack_after, as_of - created_at)
    escalations = min(MAX_ESCALATION_LEVEL - 1, int(waited / target.acknowledge_within))
    return Lifecycle(
        acknowledged_at=_reached(created_at + ack_after, as_of),
        mitigated_at=_mitigation(rng, created_at + mitigate_after, as_of),
        resolved_at=_reached(created_at + resolve_after, as_of),
        escalation_level=escalations + 1,
        ack_due_at=created_at + target.acknowledge_within * (escalations + 1),
        acknowledgement_breached=waited > target.acknowledge_within,
    )


def _ack_median_fraction(source: DetectionSource) -> float:
    if source.is_monitoring:
        return ALERTED_ACK_MEDIAN_FRACTION
    return MANUAL_ACK_MEDIAN_FRACTION


def _duration(rng: np.random.Generator, target: timedelta, median_fraction: float) -> timedelta:
    median_seconds = target.total_seconds() * median_fraction
    seconds = rng.lognormal(mean=np.log(median_seconds), sigma=DURATION_SIGMA)
    return timedelta(seconds=round(float(seconds)))


def _reached(moment: datetime, as_of: datetime) -> datetime | None:
    if moment > as_of:
        return None
    return moment


def _mitigation(rng: np.random.Generator, moment: datetime, as_of: datetime) -> datetime | None:
    if rng.random() >= MITIGATION_SHARE:
        return None
    return _reached(moment, as_of)


def _root_cause(family: IssueFamily, lifecycle: Lifecycle) -> str | None:
    if lifecycle.resolved_at is None:
        return None
    return family.root_cause


def _status(lifecycle: Lifecycle) -> IncidentStatus:
    if lifecycle.resolved_at is not None:
        return IncidentStatus.RESOLVED
    if lifecycle.mitigated_at is not None:
        return IncidentStatus.MITIGATED
    if lifecycle.acknowledged_at is not None:
        return IncidentStatus.ACKNOWLEDGED
    return IncidentStatus.TRIGGERED


def _assignee(rng: np.random.Generator, created_at: datetime, lifecycle: Lifecycle) -> str | None:
    if lifecycle.acknowledged_at is None:
        return None
    week = int((created_at - ROTATION_ANCHOR) / timedelta(weeks=1))
    if rng.random() < SECONDARY_SHARE:
        return ENGINEERS[(week + 1) % len(ENGINEERS)]
    return ENGINEERS[week % len(ENGINEERS)]


def _text(rng: np.random.Generator, family: IssueFamily, service_id: str) -> tuple[str, str]:
    values = {
        "service": service_id,
        "region": REGIONS[int(rng.integers(0, len(REGIONS)))],
        "latency": int(rng.choice([800, 1200, 1500, 2000, 3000])),
        "pct": int(rng.integers(40, 90)),
        "minutes": int(rng.integers(10, 120)),
        "count": int(rng.choice([100, 200, 500, 1000, 5000])),
        "days": int(rng.integers(3, 30)),
    }
    return family.title.format(**values), family.description.format(**values)
