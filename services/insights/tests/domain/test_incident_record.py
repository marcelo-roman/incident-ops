from dataclasses import replace
from datetime import UTC, date, datetime, timedelta
from typing import Any
from uuid import uuid4

import pytest

from incident_insights.domain.incidents.sla import SlaOutcome, SlaState
from incident_insights.domain.incidents.timeline import TimelineEntry, TimelineKind
from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.severity import Severity
from tests.builders import AS_OF, make_record


def test_response_times_are_measured_from_creation() -> None:
    record = make_record(ack_after=timedelta(minutes=12), resolve_after=timedelta(hours=3))

    assert record.time_to_acknowledge() == timedelta(minutes=12)
    assert record.time_to_resolve() == timedelta(hours=3)
    assert record.minutes_to_acknowledge() == 12
    assert record.minutes_to_resolve() == 180


def test_open_record_has_no_resolution_time() -> None:
    record = make_record(ack_after=None, resolve_after=None)

    assert record.time_to_acknowledge() is None
    assert record.minutes_to_resolve() is None


def test_compliant_record_is_met() -> None:
    record = make_record()

    assert record.complies_with_sla()
    assert record.sla_outcome(AS_OF) is SlaOutcome.MET
    assert record.is_settled(AS_OF)


def test_breach_flag_decides_acknowledgement_not_ack_due_at() -> None:
    flagged = make_record(acknowledgement_breached=True)
    moved_deadline = replace(make_record(), acknowledge_due_at=AS_OF - timedelta(days=30))

    assert flagged.acknowledgement_outcome() is SlaOutcome.BREACHED
    assert not flagged.complies_with_sla()
    assert moved_deadline.acknowledgement_outcome() is SlaOutcome.MET
    assert moved_deadline.complies_with_sla()


def test_resolution_without_acknowledgement_does_not_comply() -> None:
    record = make_record(ack_after=None, resolve_after=timedelta(minutes=20))

    assert record.acknowledgement_outcome() is SlaOutcome.BREACHED
    assert not record.complies_with_sla()


def test_late_resolution_breaches() -> None:
    record = make_record(severity=Severity.SEV1, resolve_after=timedelta(hours=5))

    assert record.resolution_outcome(AS_OF) is SlaOutcome.BREACHED
    assert record.resolution_overrun(AS_OF) == timedelta(hours=1)
    assert record.resolved_later_than_policy()


def test_open_record_inside_deadlines_is_pending() -> None:
    record = make_record(
        created_at=AS_OF - timedelta(minutes=5), ack_after=None, resolve_after=None
    )

    assert record.sla_outcome(AS_OF) is SlaOutcome.PENDING
    assert not record.is_settled(AS_OF)


def test_open_record_past_resolve_deadline_is_breached() -> None:
    record = make_record(created_at=AS_OF - timedelta(hours=10), resolve_after=None)

    assert record.resolution_outcome(AS_OF) is SlaOutcome.BREACHED
    assert record.resolution_overrun(AS_OF) == timedelta(hours=2)


def test_detection_off_hours_and_week() -> None:
    record = make_record(
        source=DetectionSource.ALERTMANAGER, created_at=datetime(2026, 9, 23, 4, tzinfo=UTC)
    )

    assert record.detected_by_monitoring()
    assert record.created_off_hours()
    assert record.week_start() == date(2026, 9, 21)


def test_responder_defaults_to_unassigned() -> None:
    assert make_record(assignee=None).responder == "unassigned"
    assert make_record(assignee="bruno").responder == "bruno"


def test_chronology_derives_lifecycle_when_no_timeline_is_recorded() -> None:
    record = make_record(
        mitigate_after=timedelta(hours=1),
        root_cause="Index dropped",
        source=DetectionSource.ALERTMANAGER,
    )

    timeline = record.chronology()

    assert [entry.kind for entry in timeline] == [
        TimelineKind.TRIGGERED,
        TimelineKind.ACKNOWLEDGED,
        TimelineKind.MITIGATED,
        TimelineKind.RESOLVED,
    ]
    assert timeline[0].actor == "Alertmanager"
    assert timeline[-1].message == "Root cause: Index dropped"
    assert record.chronology() == timeline


def test_chronology_orders_the_recorded_timeline() -> None:
    record = make_record()
    late = TimelineEntry(
        uuid4(), record.created_at + timedelta(hours=1), TimelineKind.NOTE, "a", "b"
    )
    early = TimelineEntry(uuid4(), record.created_at, TimelineKind.TRIGGERED, "a", "c")

    ordered = replace(record, timeline=(late, early)).chronology()

    assert ordered == (early, late)


def test_open_record_lifecycle_only_has_the_trigger() -> None:
    record = make_record(ack_after=None, resolve_after=None, assignee=None)

    assert [entry.kind for entry in record.chronology()] == [TimelineKind.TRIGGERED]
    assert make_record(resolve_after=timedelta(hours=1)).chronology()[-1].message == (
        "Incident resolved"
    )


def test_records_are_entities_compared_by_identity() -> None:
    record = make_record()

    assert record == replace(record, title="Renamed")
    assert record != make_record()
    assert len({record, replace(record, number=9)}) == 1
    assert record.__eq__(object()) is NotImplemented


@pytest.mark.parametrize(
    ("changes", "message"),
    [
        ({"escalation_level": 0}, "level 1"),
        ({"created_at": datetime(2026, 9, 1)}, "timezone"),
        ({"resolved_at": AS_OF - timedelta(days=30)}, "resolved before"),
        ({"acknowledged_at": AS_OF - timedelta(days=30)}, "acknowledged before"),
    ],
)
def test_record_invariants(changes: dict[str, Any], message: str) -> None:
    with pytest.raises(ValueError, match=message):
        replace(make_record(), **changes)


def test_timeline_entries_need_aware_timestamps() -> None:
    with pytest.raises(ValueError, match="timezone"):
        TimelineEntry(uuid4(), datetime(2026, 9, 1), TimelineKind.NOTE, "a", "b")


CREATED = datetime(2026, 9, 1, 12, 0, tzinfo=UTC)


def _state(
    as_of: datetime,
    acknowledged_at: datetime | None = None,
    resolved_at: datetime | None = None,
    acknowledgement_breached: bool = False,
) -> SlaState:
    return SlaState.evaluate(
        created_at=CREATED,
        acknowledged_at=acknowledged_at,
        acknowledgement_breached=acknowledgement_breached,
        resolved_at=resolved_at,
        acknowledge_due_at=CREATED + timedelta(minutes=30),
        resolve_due_at=CREATED + timedelta(hours=8),
        as_of=as_of,
    )


ACKNOWLEDGED = CREATED + timedelta(minutes=5)
LATER = CREATED + timedelta(days=1)


def test_live_state_once_resolved_follows_compliance() -> None:
    in_time = CREATED + timedelta(hours=2)

    assert _state(LATER, ACKNOWLEDGED, in_time) is SlaState.MET
    assert _state(LATER, ACKNOWLEDGED, CREATED + timedelta(hours=9)) is SlaState.BREACHED
    assert _state(LATER, ACKNOWLEDGED, in_time, acknowledgement_breached=True) is (
        SlaState.BREACHED
    )
    assert _state(LATER, None, in_time) is SlaState.BREACHED


def test_live_state_while_open() -> None:
    assert _state(CREATED + timedelta(minutes=31)) is SlaState.BREACHED
    assert _state(CREATED + timedelta(hours=9), ACKNOWLEDGED) is SlaState.BREACHED
    assert _state(CREATED + timedelta(minutes=25)) is SlaState.AT_RISK
    assert _state(CREATED + timedelta(hours=1), ACKNOWLEDGED) is SlaState.ON_TRACK
