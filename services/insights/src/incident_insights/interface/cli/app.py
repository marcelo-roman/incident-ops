from pathlib import Path
from typing import Annotated

import typer

from incident_insights.application.clock import fixed_clock, utc_now
from incident_insights.application.ports import Clock
from incident_insights.infrastructure.composition import build_use_cases
from incident_insights.infrastructure.sample.csv_writer import write_incidents_csv
from incident_insights.infrastructure.sample.generator import (
    DEFAULT_DAYS,
    DEFAULT_END,
    DEFAULT_SEED,
    SampleSpec,
    generate_incidents,
)
from incident_insights.infrastructure.settings import DataSourceKind, Settings
from incident_insights.interface.cli.options import parse_moment
from incident_insights.interface.reports.renderer import ReportFormat, render_report

app = typer.Typer(help="KTLO analytics for Incident Ops.", no_args_is_help=True)

DEFAULT_SAMPLE_PATH = Path("data/sample_incidents.csv")


@app.command()
def report(
    *,
    days: Annotated[int, typer.Option(min=1, max=730, help="Trailing window in days.")] = 30,
    report_format: Annotated[
        ReportFormat, typer.Option("--format", help="Output format.")
    ] = ReportFormat.MARKDOWN,
    source: Annotated[
        DataSourceKind | None,
        typer.Option(help="Incident source; defaults to INSIGHTS_DATA_SOURCE."),
    ] = None,
    csv_path: Annotated[Path | None, typer.Option(help="CSV file for the csv source.")] = None,
    as_of: Annotated[
        str | None, typer.Option(help="End of the window (ISO-8601, UTC). Defaults to now.")
    ] = None,
    output: Annotated[Path | None, typer.Option(help="Write to a file instead of stdout.")] = None,
) -> None:
    settings = _settings(source, csv_path)
    use_cases = build_use_cases(settings, _clock(as_of))
    content = render_report(use_cases.build_report.execute(days), report_format)
    if output is None:
        typer.echo(content, nl=False)
        return
    output.write_text(content, encoding="utf-8")
    typer.echo(f"Report written to {output}", err=True)


@app.command("generate-sample")
def generate_sample(
    *,
    output: Annotated[Path, typer.Option(help="CSV file to write.")] = DEFAULT_SAMPLE_PATH,
    end: Annotated[str, typer.Option(help="End of the generated history (ISO-8601).")] = (
        DEFAULT_END.date().isoformat()
    ),
    days: Annotated[int, typer.Option(min=7, help="Days of history.")] = DEFAULT_DAYS,
    seed: Annotated[int, typer.Option(help="Random seed.")] = DEFAULT_SEED,
) -> None:
    spec = SampleSpec(end=parse_moment(end) or DEFAULT_END, days=days, seed=seed)
    incidents = generate_incidents(spec)
    write_incidents_csv(incidents, output)
    typer.echo(f"Wrote {len(incidents)} incidents to {output}", err=True)


def _settings(source: DataSourceKind | None, csv_path: Path | None) -> Settings:
    overrides: dict[str, object] = {}
    if source is not None:
        overrides["insights_data_source"] = source
    if csv_path is not None:
        overrides["insights_csv_path"] = csv_path
    return Settings().model_copy(update=overrides)


def _clock(as_of: str | None) -> Clock:
    moment = parse_moment(as_of)
    if moment is None:
        return utc_now
    return fixed_clock(moment)
