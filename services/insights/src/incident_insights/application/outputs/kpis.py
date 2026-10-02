from datetime import date
from typing import Self

from incident_insights.application.outputs.common import (
    DurationStatsOutput,
    Output,
    WindowOutput,
    percent,
)
from incident_insights.domain.kpis.report import (
    DetectionCoverage,
    KpiGroup,
    KpiReport,
    KpiSummary,
    SlaCompliance,
    SourceKpi,
    WeeklyKpi,
)


class SlaComplianceOutput(Output):
    evaluated: int
    breached: int
    acknowledge_pct: float | None
    resolve_pct: float | None
    overall_pct: float | None

    @classmethod
    def of(cls, compliance: SlaCompliance) -> Self:
        return cls(
            evaluated=compliance.evaluated,
            breached=compliance.breached,
            acknowledge_pct=percent(compliance.acknowledgement),
            resolve_pct=percent(compliance.resolution),
            overall_pct=percent(compliance.overall),
        )


class KpiSummaryOutput(Output):
    incidents: int
    mtta: DurationStatsOutput
    mttr: DurationStatsOutput
    sla: SlaComplianceOutput

    @classmethod
    def of(cls, summary: KpiSummary) -> Self:
        return cls(
            incidents=summary.incidents,
            mtta=DurationStatsOutput.of(summary.time_to_acknowledge),
            mttr=DurationStatsOutput.of(summary.time_to_resolve),
            sla=SlaComplianceOutput.of(summary.sla),
        )


class KpiGroupOutput(KpiSummaryOutput):
    service_id: str | None = None
    severity: str | None = None

    @classmethod
    def of_group(cls, group: KpiGroup) -> Self:
        summary = KpiSummaryOutput.of(group.summary)
        return cls(
            **summary.model_dump(),
            service_id=_text(group.service),
            severity=_text(group.severity),
        )


class WeeklyKpiOutput(Output):
    week_start: date
    incidents: int
    mtta_median_minutes: float | None
    mttr_median_minutes: float | None
    sla_compliance_pct: float | None

    @classmethod
    def of(cls, week: WeeklyKpi) -> Self:
        return cls(
            week_start=week.week_start,
            incidents=week.summary.incidents,
            mtta_median_minutes=week.summary.time_to_acknowledge.median_minutes,
            mttr_median_minutes=week.summary.time_to_resolve.median_minutes,
            sla_compliance_pct=percent(week.summary.sla.overall),
        )


class SourceKpiOutput(Output):
    source: str
    incidents: int
    share_pct: float | None
    mtta: DurationStatsOutput

    @classmethod
    def of(cls, item: SourceKpi) -> Self:
        return cls(
            source=item.source.value,
            incidents=item.incidents,
            share_pct=percent(item.share),
            mtta=DurationStatsOutput.of(item.time_to_acknowledge),
        )


class DetectionCoverageOutput(Output):
    incidents: int
    alerting_pct: float | None
    by_source: list[SourceKpiOutput]

    @classmethod
    def of(cls, coverage: DetectionCoverage) -> Self:
        return cls(
            incidents=coverage.incidents,
            alerting_pct=percent(coverage.monitoring_share),
            by_source=[SourceKpiOutput.of(item) for item in coverage.by_source],
        )


class KpiReportOutput(Output):
    window: WindowOutput
    overall: KpiSummaryOutput
    detection: DetectionCoverageOutput
    by_service: list[KpiGroupOutput]
    by_severity: list[KpiGroupOutput]
    by_service_severity: list[KpiGroupOutput]
    weekly: list[WeeklyKpiOutput]

    @classmethod
    def of(cls, report: KpiReport) -> Self:
        return cls(
            window=WindowOutput.of(report.window),
            overall=KpiSummaryOutput.of(report.overall),
            detection=DetectionCoverageOutput.of(report.detection),
            by_service=_groups(report.by_service),
            by_severity=_groups(report.by_severity),
            by_service_severity=_groups(report.by_service_severity),
            weekly=[WeeklyKpiOutput.of(week) for week in report.weekly],
        )


def _groups(groups: tuple[KpiGroup, ...]) -> list[KpiGroupOutput]:
    return [KpiGroupOutput.of_group(group) for group in groups]


def _text(value: object | None) -> str | None:
    if value is None:
        return None
    return str(value)
