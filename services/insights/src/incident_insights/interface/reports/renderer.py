from datetime import timedelta
from enum import StrEnum

from jinja2 import Environment, PackageLoader, StrictUndefined, select_autoescape

from incident_insights.application.outputs.reports import KtloReportOutput
from incident_insights.domain.shared.duration_text import describe_duration


class ReportFormat(StrEnum):
    MARKDOWN = "md"
    HTML = "html"


def render_report(report: KtloReportOutput, report_format: ReportFormat) -> str:
    template = _environment().get_template(f"ktlo.{report_format.value}")
    return template.render(report=report)


def _environment() -> Environment:
    environment = Environment(
        loader=PackageLoader("incident_insights.interface.reports", "templates"),
        autoescape=select_autoescape(enabled_extensions=("html",), default_for_string=False),
        undefined=StrictUndefined,
        trim_blocks=True,
        lstrip_blocks=True,
        keep_trailing_newline=True,
    )
    environment.filters["minutes"] = minutes_label
    environment.filters["percent"] = percent_label
    environment.filters["cell"] = _markdown_cell
    return environment


def minutes_label(minutes: float | None) -> str:
    if minutes is None:
        return "n/a"
    return describe_duration(timedelta(minutes=minutes))


def percent_label(value: float | None) -> str:
    if value is None:
        return "n/a"
    return f"{value:.1f}%"


def _markdown_cell(value: object) -> str:
    return str(value).replace("|", "\\|").replace("\n", " ")
