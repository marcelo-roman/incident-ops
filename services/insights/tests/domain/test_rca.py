from dataclasses import replace
from datetime import timedelta
from typing import cast
from uuid import uuid4

import pytest

from incident_insights.domain.incidents.timeline import TimelineEntry, TimelineKind
from incident_insights.domain.rca.draft import (
    ActionItem,
    InvalidRcaDraftError,
    Priority,
    RcaDraft,
    RcaTimelineEvent,
)
from incident_insights.domain.rca.rca_draft_composer import RcaDraftComposer
from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.severity import Severity
from tests.builders import make_record

COMPOSER = RcaDraftComposer()


def test_priority_follows_severity() -> None:
    assert [Priority.for_severity(severity) for severity in Severity] == [
        Priority.P1,
        Priority.P1,
        Priority.P2,
        Priority.P3,
    ]


def test_draft_requires_summary_and_impact() -> None:
    with pytest.raises(InvalidRcaDraftError, match="summary"):
        RcaDraft(" ", "impact", (), (), ())
    with pytest.raises(InvalidRcaDraftError, match="impact"):
        RcaDraft("summary", "", (), (), ())
    with pytest.raises(InvalidRcaDraftError, match="blank"):
        RcaDraft("summary", "impact", (), ("",), ())


def test_action_items_need_title_owner_and_valid_priority() -> None:
    with pytest.raises(InvalidRcaDraftError, match="title"):
        ActionItem("", "owners", Priority.P1)
    with pytest.raises(InvalidRcaDraftError, match="owner"):
        ActionItem("Fix it", " ", Priority.P1)
    with pytest.raises(InvalidRcaDraftError, match="priority"):
        ActionItem("Fix it", "owners", cast(Priority, "P9"))
    with pytest.raises(InvalidRcaDraftError, match="description"):
        RcaTimelineEvent("2026-09-01T00:00:00Z", "")


def test_composer_flags_breaches_escalation_and_manual_detection() -> None:
    record = make_record(
        severity=Severity.SEV1,
        ack_after=timedelta(minutes=40),
        resolve_after=timedelta(hours=6),
        escalation_level=2,
        root_cause="Connection leak",
    )

    draft = COMPOSER.compose(record)

    factors = " ".join(draft.contributing_factors)
    assert "Recorded root cause: Connection leak" in factors
    assert "No alert fired" in factors
    assert "Acknowledgement SLA breached: acknowledged after 40m against the 15m target" in factors
    assert "escalated to level 2" in factors
    assert "Resolution took 6h" in factors
    assert {item.priority for item in draft.action_items} == {Priority.P1, Priority.P2}
    assert len(draft.action_items) == 4
    assert "resolved after 6h" in draft.summary
    assert "breached its SLA (acknowledgement, resolution)" in draft.impact


def test_compliant_incident_says_so() -> None:
    draft = COMPOSER.compose(make_record())

    assert "The incident met its SLA." in draft.impact
    assert not any("SLA breached" in factor for factor in draft.contributing_factors)


def test_open_monitored_incident_has_ongoing_impact() -> None:
    record = make_record(
        severity=Severity.SEV4,
        ack_after=None,
        resolve_after=None,
        source=DetectionSource.AZURE_MONITOR,
    )

    draft = COMPOSER.compose(record)

    assert "not yet resolved" in draft.summary
    assert "Impact is ongoing" in draft.impact
    assert draft.contributing_factors[0].startswith("The incident record does not show")
    assert [item.priority for item in draft.action_items] == [Priority.P3]
    assert draft.timeline[0].event == "Triggered: Checkout API latency above 1200ms (AzureMonitor)"


def test_escalated_open_incident_reports_missing_acknowledgement() -> None:
    draft = COMPOSER.compose(make_record(ack_after=None, resolve_after=None, escalation_level=3))

    assert "Acknowledgement SLA breached: no acknowledgement within the 30m target." in (
        draft.contributing_factors
    )
    assert "Live SLA state" in draft.impact


def test_mitigation_window_is_the_impact_duration() -> None:
    draft = COMPOSER.compose(make_record(mitigate_after=timedelta(minutes=45)))

    assert "Impact lasted about 45m" in draft.impact


def test_recorded_timeline_is_used_in_chronological_order() -> None:
    record = make_record(mitigate_after=timedelta(minutes=30))
    note = TimelineEntry(
        uuid4(),
        record.created_at + timedelta(minutes=20),
        TimelineKind.NOTE,
        "bruno",
        "Rolled back",
    )
    recorded = replace(record, timeline=(*reversed(record.chronology()), note))

    draft = COMPOSER.compose(recorded)

    times = [event.at for event in draft.timeline]
    assert times == sorted(times)
    assert "Note: Rolled back (bruno)" in [event.event for event in draft.timeline]
