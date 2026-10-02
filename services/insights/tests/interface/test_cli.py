from pathlib import Path

from typer.testing import CliRunner

from incident_insights.interface.cli.app import app

runner = CliRunner()


def _report(sample_csv: Path, *extra: str) -> str:
    result = runner.invoke(
        app,
        [
            "report",
            "--source",
            "csv",
            "--csv-path",
            str(sample_csv),
            "--days",
            "30",
            "--as-of",
            "2026-09-28",
            *extra,
        ],
    )
    assert result.exit_code == 0, result.output
    return result.stdout


def test_markdown_report_has_every_section(sample_csv: Path) -> None:
    output = _report(sample_csv)

    assert output.startswith("# KTLO report: 2026-08-29 to 2026-09-28")
    for section in (
        "## Headline",
        "## Detection",
        "## Top recurring issues",
        "## SLA breaches",
        "## MTTR by service",
        "## On-call load",
    ):
        assert section in output


def test_html_report_is_written_to_file(sample_csv: Path, tmp_path: Path) -> None:
    target = tmp_path / "report.html"

    _report(sample_csv, "--format", "html", "--output", str(target))

    content = target.read_text(encoding="utf-8")
    assert content.startswith("<!doctype html>")
    assert "Detected by monitoring" in content


def test_invalid_as_of_is_rejected(sample_csv: Path) -> None:
    result = runner.invoke(app, ["report", "--source", "csv", "--as-of", "yesterday"])

    assert result.exit_code != 0


def test_generate_sample_reproduces_committed_csv(sample_csv: Path, tmp_path: Path) -> None:
    target = tmp_path / "incidents.csv"

    result = runner.invoke(app, ["generate-sample", "--output", str(target)])

    assert result.exit_code == 0, result.output
    assert target.read_bytes() == sample_csv.read_bytes()


def test_generate_sample_respects_seed_and_window(tmp_path: Path) -> None:
    first = tmp_path / "a.csv"
    second = tmp_path / "b.csv"
    arguments = ["generate-sample", "--days", "14", "--seed", "7", "--end", "2026-06-01T00:00:00Z"]

    runner.invoke(app, [*arguments, "--output", str(first)])
    runner.invoke(app, [*arguments, "--output", str(second)])

    assert first.read_bytes() == second.read_bytes()
    assert len(first.read_text(encoding="utf-8").splitlines()) > 1
