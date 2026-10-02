from datetime import UTC, date, datetime, timedelta
from zoneinfo import ZoneInfo

import pytest

from incident_insights.domain.incidents.business_hours import ON_CALL_BUSINESS_HOURS, BusinessHours
from incident_insights.domain.incidents.sla import (
    STANDARD_SLA_POLICY,
    SlaOutcome,
    SlaPolicy,
    SlaTargets,
)
from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.duration_stats import DurationStats
from incident_insights.domain.shared.duration_text import describe_duration
from incident_insights.domain.shared.percentage import Percentage
from incident_insights.domain.shared.reporting_window import ReportingWindow, week_start_of
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.domain.shared.severity import Severity

MONDAY = datetime(2026, 9, 21, tzinfo=UTC)


def test_severity_knows_high_severities() -> None:
    assert [severity.is_high for severity in Severity] == [True, True, False, False]


def test_detection_source_knows_monitoring() -> None:
    assert not DetectionSource.MANUAL.is_monitoring
    assert DetectionSource.ALERTMANAGER.is_monitoring
    assert DetectionSource.AZURE_MONITOR.is_monitoring


@pytest.mark.parametrize("value", ["checkout", "payments-gateway", "platform"])
def test_service_id_accepts_slugs(value: str) -> None:
    assert str(ServiceId(value)) == value


@pytest.mark.parametrize("value", ["", "Checkout", "payments gateway", "-search", "a--b"])
def test_service_id_rejects_non_slugs(value: str) -> None:
    with pytest.raises(ValueError, match="slug"):
        ServiceId(value)


def test_service_ids_compare_by_value() -> None:
    assert ServiceId("search") == ServiceId("search")
    assert sorted([ServiceId("search"), ServiceId("checkout")])[0] == ServiceId("checkout")


def test_percentage_share_and_fraction_round_to_one_decimal() -> None:
    assert Percentage.share(1, 3) == Percentage(33.3)
    assert Percentage.share(0, 0) is None
    assert Percentage.from_fraction(0.6666) == Percentage(66.7)


@pytest.mark.parametrize("value", [-0.1, 100.1])
def test_percentage_rejects_out_of_range(value: float) -> None:
    with pytest.raises(ValueError, match="percentage"):
        Percentage(value)


def test_duration_stats_reports_median_and_linear_p90() -> None:
    stats = DurationStats.of_minutes([5, 10, 20, 60])

    assert stats == DurationStats(samples=4, median_minutes=15.0, p90_minutes=48.0)


def test_duration_stats_without_samples_is_empty() -> None:
    assert DurationStats.of_minutes([]) == DurationStats(0, None, None)


def test_duration_stats_invariants() -> None:
    with pytest.raises(ValueError, match="median"):
        DurationStats(samples=2, median_minutes=None, p90_minutes=None)
    with pytest.raises(ValueError, match="negative"):
        DurationStats(samples=-1, median_minutes=None, p90_minutes=None)


def test_reporting_window_is_trailing_and_half_open() -> None:
    window = ReportingWindow.trailing(7, MONDAY)

    assert window.days == 7
    assert window.contains(window.start)
    assert not window.contains(window.end)


def test_reporting_window_lists_monday_week_starts() -> None:
    window = ReportingWindow.trailing(14, MONDAY + timedelta(days=2))

    assert window.week_starts() == (date(2026, 9, 7), date(2026, 9, 14), date(2026, 9, 21))


def test_reporting_window_invariants() -> None:
    with pytest.raises(ValueError, match="precede"):
        ReportingWindow(MONDAY, MONDAY)
    with pytest.raises(ValueError, match="timezone"):
        ReportingWindow(datetime(2026, 9, 1), datetime(2026, 9, 2))
    with pytest.raises(ValueError, match="one day"):
        ReportingWindow.trailing(0, MONDAY)


def test_week_start_is_monday_in_utc() -> None:
    sunday_evening_in_new_york = datetime(2026, 9, 27, 22, 0, tzinfo=ZoneInfo("America/New_York"))

    assert week_start_of(sunday_evening_in_new_york) == date(2026, 9, 28)


@pytest.mark.parametrize(
    ("duration", "text"),
    [
        (timedelta(0), "0m"),
        (timedelta(minutes=45), "45m"),
        (timedelta(days=1, hours=2, minutes=3), "1d 2h 3m"),
        (timedelta(seconds=-30), "0m"),
    ],
)
def test_describe_duration(duration: timedelta, text: str) -> None:
    assert describe_duration(duration) == text


def test_standard_policy_follows_the_contract() -> None:
    targets = STANDARD_SLA_POLICY.targets_for(Severity.SEV1)

    assert targets == SlaTargets(timedelta(minutes=15), timedelta(hours=4))


def test_sla_targets_and_policy_invariants() -> None:
    with pytest.raises(ValueError, match="positive"):
        SlaTargets(timedelta(0), timedelta(hours=1))
    with pytest.raises(ValueError, match="exceed"):
        SlaTargets(timedelta(hours=2), timedelta(hours=1))
    with pytest.raises(ValueError, match="lacks"):
        SlaPolicy({Severity.SEV1: SlaTargets(timedelta(minutes=1), timedelta(hours=1))})


def test_sla_outcome_judgement_prefers_breach() -> None:
    assert SlaOutcome.judge(breached=True, met=True) is SlaOutcome.BREACHED
    assert SlaOutcome.judge(breached=False, met=True) is SlaOutcome.MET
    assert SlaOutcome.judge(breached=False, met=False) is SlaOutcome.PENDING
    assert not SlaOutcome.PENDING.is_decided


def test_business_hours_exclude_nights_and_weekends() -> None:
    wednesday = datetime(2026, 9, 23, tzinfo=UTC)

    assert not ON_CALL_BUSINESS_HOURS.excludes(wednesday.replace(hour=15))
    assert ON_CALL_BUSINESS_HOURS.excludes(wednesday.replace(hour=4))
    assert ON_CALL_BUSINESS_HOURS.excludes(datetime(2026, 9, 26, 15, tzinfo=UTC))
    with pytest.raises(ValueError, match="range"):
        BusinessHours(ZoneInfo("UTC"), 18, 9)
