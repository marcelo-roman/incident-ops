from datetime import UTC, datetime, timedelta

import pytest

from incident_insights.domain.incidents.status import IncidentStatus
from incident_insights.domain.kpis.kpi_calculator import KpiCalculator
from incident_insights.domain.recurring.recurring_issue_detector import RecurringIssueDetector
from incident_insights.domain.reports.ktlo_report import OnCallLoad, SlaBreach
from incident_insights.domain.reports.ktlo_report_composer import (
    MAX_BREACHES,
    KtloReportComposer,
)
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.domain.shared.severity import Severity
from tests.builders import AS_OF, history_of, make_record, window_of

COMPOSER = KtloReportComposer(KpiCalculator(), RecurringIssueDetector())
WINDOW = window_of(7)


def test_services_are_ordered_by_slowest_resolution() -> None:
    history = history_of(
        make_record(service="search", resolve_after=timedelta(hours=1)),
        make_record(service="checkout", resolve_after=timedelta(hours=7)),
    )

    report = COMPOSER.compose(history, WINDOW)

    assert [group.service for group in report.mttr_by_service] == [
        ServiceId("checkout"),
        ServiceId("search"),
    ]
    assert report.generated_at == AS_OF


def test_breach_list_is_capped_but_total_is_kept() -> None:
    history = history_of(
        *(
            make_record(number=index, severity=Severity.SEV1, ack_after=timedelta(hours=1))
            for index in range(MAX_BREACHES + 3)
        )
    )

    report = COMPOSER.compose(history, WINDOW)

    assert len(report.breaches) == MAX_BREACHES
    assert report.total_breaches == MAX_BREACHES + 3


def test_breaches_by_severity_with_targets_and_overrun() -> None:
    history = history_of(
        make_record(number=1, severity=Severity.SEV3, ack_after=timedelta(hours=5)),
        make_record(
            number=2,
            severity=Severity.SEV1,
            ack_after=timedelta(minutes=30),
            resolve_after=timedelta(hours=5),
            assignee=None,
        ),
        make_record(number=3),
    )

    breaches = COMPOSER.breaches(history, AS_OF)

    assert [breach.number for breach in breaches] == [2, 1]
    assert breaches[0].breached_targets == ("acknowledge", "resolve")
    assert breaches[0].resolve_overrun_minutes == 60
    assert breaches[0].assignee is None
    assert breaches[1].breached_targets == ("acknowledge",)
    assert breaches[1].resolve_overrun_minutes is None


def test_on_call_load_counts_high_severity_off_hours_and_escalations() -> None:
    business_hours = datetime(2026, 9, 23, 15, 0, tzinfo=UTC)
    night = datetime(2026, 9, 23, 4, 0, tzinfo=UTC)
    history = history_of(
        make_record(assignee="alice", severity=Severity.SEV1, created_at=night),
        make_record(assignee="alice", created_at=business_hours, escalation_level=2),
        make_record(assignee="bruno", severity=Severity.SEV4, created_at=business_hours),
        make_record(assignee=None, ack_after=None, created_at=business_hours),
    )

    load = {item.assignee: item for item in COMPOSER.on_call_load(history)}

    assert load["alice"] == OnCallLoad("alice", 2, 2, 1, 1)
    assert load["bruno"].high_severity == 0
    assert load["unassigned"].incidents == 1
    assert COMPOSER.on_call_load(history_of()) == ()


def test_report_value_invariants() -> None:
    with pytest.raises(ValueError, match="missed target"):
        SlaBreach(
            1, "t", ServiceId("search"), Severity.SEV1, IncidentStatus.RESOLVED, None, (), None
        )
    with pytest.raises(ValueError, match="exceed"):
        OnCallLoad("alice", 1, 2, 0, 0)
