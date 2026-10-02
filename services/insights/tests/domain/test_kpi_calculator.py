from datetime import timedelta

from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.kpis.kpi_calculator import KpiCalculator
from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.percentage import Percentage
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.domain.shared.severity import Severity
from tests.builders import AS_OF, history_of, make_record, window_of

CALCULATOR = KpiCalculator()
WINDOW = window_of(28)


def _history() -> IncidentHistory:
    acknowledgements = ((5, "checkout"), (10, "checkout"), (20, "search"), (60, "search"))
    return history_of(
        *(
            make_record(number=index, ack_after=timedelta(minutes=minutes), service=service)
            for index, (minutes, service) in enumerate(acknowledgements)
        )
    )


def test_summary_reports_median_and_p90() -> None:
    summary = CALCULATOR.summarize(_history(), AS_OF)

    assert summary.incidents == 4
    assert summary.time_to_acknowledge.median_minutes == 15
    assert summary.time_to_acknowledge.p90_minutes == 48
    assert summary.time_to_resolve.median_minutes == 120


def test_sla_compliance_splits_targets() -> None:
    sla = CALCULATOR.summarize(_history(), AS_OF).sla

    assert sla.acknowledgement == Percentage(75.0)
    assert sla.resolution == Percentage(100.0)
    assert sla.overall == Percentage(75.0)
    assert (sla.evaluated, sla.breached) == (4, 1)


def test_report_groups_by_service_and_severity() -> None:
    report = CALCULATOR.report(_history(), WINDOW)

    assert [group.service for group in report.by_service] == [
        ServiceId("checkout"),
        ServiceId("search"),
    ]
    assert [group.severity for group in report.by_severity] == [Severity.SEV2]
    assert len(report.by_service_severity) == 2
    assert report.by_service[0].summary.time_to_acknowledge.median_minutes == 7.5


def test_weekly_trend_covers_every_week_of_the_window() -> None:
    report = CALCULATOR.report(_history(), WINDOW)

    assert len(report.weekly) == 5
    assert sum(week.summary.incidents for week in report.weekly) == 4
    assert report.weekly[0].summary.time_to_acknowledge.median_minutes is None


def test_detection_coverage_reports_monitoring_share_and_mtta_by_source() -> None:
    history = history_of(
        make_record(source=DetectionSource.ALERTMANAGER, ack_after=timedelta(minutes=4)),
        make_record(source=DetectionSource.AZURE_MONITOR, ack_after=timedelta(minutes=6)),
        make_record(source=DetectionSource.MANUAL, ack_after=timedelta(minutes=30)),
        make_record(source=DetectionSource.MANUAL, ack_after=timedelta(minutes=50)),
    )

    detection = CALCULATOR.detection(history)
    by_source = {item.source: item for item in detection.by_source}

    assert detection.monitoring_share == Percentage(50.0)
    assert by_source[DetectionSource.MANUAL].time_to_acknowledge.median_minutes == 40
    assert by_source[DetectionSource.ALERTMANAGER].share == Percentage(25.0)


def test_empty_history_yields_empty_metrics() -> None:
    report = CALCULATOR.report(history_of(), WINDOW)

    assert report.overall.incidents == 0
    assert report.overall.time_to_acknowledge.median_minutes is None
    assert report.overall.sla.overall is None
    assert report.detection.monitoring_share is None
    assert report.by_service == ()
