from datetime import datetime
from typing import Self

from incident_insights.application.outputs.common import Output, WindowOutput
from incident_insights.application.outputs.kpis import (
    DetectionCoverageOutput,
    KpiGroupOutput,
    KpiSummaryOutput,
)
from incident_insights.application.outputs.recurring import RecurringClusterOutput
from incident_insights.domain.reports.ktlo_report import KtloReport, OnCallLoad, SlaBreach


class SlaBreachOutput(Output):
    number: int
    title: str
    service_id: str
    severity: str
    status: str
    assignee: str | None
    breached_targets: list[str]
    resolve_overrun_minutes: float | None

    @classmethod
    def of(cls, breach: SlaBreach) -> Self:
        return cls(
            number=breach.number,
            title=breach.title,
            service_id=str(breach.service),
            severity=breach.severity.value,
            status=breach.status.value,
            assignee=breach.assignee,
            breached_targets=list(breach.breached_targets),
            resolve_overrun_minutes=breach.resolve_overrun_minutes,
        )


class OnCallLoadOutput(Output):
    assignee: str
    incidents: int
    high_severity: int
    off_hours: int
    escalated: int

    @classmethod
    def of(cls, load: OnCallLoad) -> Self:
        return cls(
            assignee=load.assignee,
            incidents=load.incidents,
            high_severity=load.high_severity,
            off_hours=load.off_hours,
            escalated=load.escalated,
        )


class KtloReportOutput(Output):
    generated_at: datetime
    window: WindowOutput
    overall: KpiSummaryOutput
    detection: DetectionCoverageOutput
    top_recurring: list[RecurringClusterOutput]
    breaches: list[SlaBreachOutput]
    total_breaches: int
    mttr_by_service: list[KpiGroupOutput]
    on_call: list[OnCallLoadOutput]

    @classmethod
    def of(cls, report: KtloReport) -> Self:
        return cls(
            generated_at=report.generated_at,
            window=WindowOutput.of(report.window),
            overall=KpiSummaryOutput.of(report.overall),
            detection=DetectionCoverageOutput.of(report.detection),
            top_recurring=[RecurringClusterOutput.of(item) for item in report.top_recurring],
            breaches=[SlaBreachOutput.of(item) for item in report.breaches],
            total_breaches=report.total_breaches,
            mttr_by_service=[KpiGroupOutput.of_group(item) for item in report.mttr_by_service],
            on_call=[OnCallLoadOutput.of(item) for item in report.on_call],
        )
