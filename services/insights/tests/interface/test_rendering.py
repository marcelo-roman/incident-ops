from datetime import timedelta

from incident_insights.application.outputs.reports import KtloReportOutput
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.kpis.kpi_calculator import KpiCalculator
from incident_insights.domain.recurring.recurring_issue_detector import RecurringIssueDetector
from incident_insights.domain.reports.ktlo_report_composer import KtloReportComposer
from incident_insights.domain.shared.severity import Severity
from incident_insights.interface.reports.renderer import (
    ReportFormat,
    minutes_label,
    percent_label,
    render_report,
)
from tests.builders import history_of, make_record, window_of

COMPOSER = KtloReportComposer(KpiCalculator(), RecurringIssueDetector())


def _report(*records: IncidentRecord) -> KtloReportOutput:
    return KtloReportOutput.of(COMPOSER.compose(history_of(*records), window_of(7)))


def test_empty_window_renders_placeholders() -> None:
    output = render_report(_report(), ReportFormat.MARKDOWN)

    assert "No recurring clusters in this window." in output
    assert "No SLA breaches in this window." in output
    assert "| 0 | n/a |" in output


def test_markdown_escapes_table_separators() -> None:
    record = make_record(
        title="Latency | errors", severity=Severity.SEV1, ack_after=timedelta(hours=1)
    )

    output = render_report(_report(record), ReportFormat.MARKDOWN)

    assert "Latency \\| errors" in output


def test_html_escapes_incident_text() -> None:
    record = make_record(title="<script>alert(1)</script>", ack_after=timedelta(hours=2))

    output = render_report(_report(record), ReportFormat.HTML)

    assert "<script>alert(1)</script>" not in output
    assert "&lt;script&gt;" in output


def test_labels() -> None:
    assert minutes_label(None) == "n/a"
    assert minutes_label(90.0) == "1h 30m"
    assert percent_label(None) == "n/a"
    assert percent_label(65.55) == "65.5%"
