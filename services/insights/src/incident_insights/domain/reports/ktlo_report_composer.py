from datetime import datetime

from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.incidents.sla import SlaOutcome
from incident_insights.domain.kpis.kpi_calculator import KpiCalculator
from incident_insights.domain.kpis.report import KpiGroup
from incident_insights.domain.recurring.recurring_issue_detector import RecurringIssueDetector
from incident_insights.domain.reports.ktlo_report import KtloReport, OnCallLoad, SlaBreach
from incident_insights.domain.shared.reporting_window import ReportingWindow

TOP_RECURRING = 5
MAX_BREACHES = 15
MINUTES_DIGITS = 1
SECONDS_PER_MINUTE = 60


class KtloReportComposer:
    def __init__(self, kpis: KpiCalculator, recurring: RecurringIssueDetector) -> None:
        self._kpis = kpis
        self._recurring = recurring

    def compose(self, history: IncidentHistory, window: ReportingWindow) -> KtloReport:
        as_of = window.end
        breaches = self.breaches(history, as_of)
        return KtloReport(
            generated_at=as_of,
            window=window,
            overall=self._kpis.summarize(history, as_of),
            detection=self._kpis.detection(history),
            top_recurring=self._recurring.detect(history, window).top(TOP_RECURRING),
            breaches=breaches[:MAX_BREACHES],
            total_breaches=len(breaches),
            mttr_by_service=tuple(
                sorted(self._kpis.by_service(history, as_of), key=_slowest_resolution_first)
            ),
            on_call=self.on_call_load(history),
        )

    def breaches(self, history: IncidentHistory, as_of: datetime) -> tuple[SlaBreach, ...]:
        breached = history.where(lambda record: record.sla_outcome(as_of) is SlaOutcome.BREACHED)
        newest_first = sorted(breached, key=lambda record: record.created_at, reverse=True)
        ordered = sorted(newest_first, key=lambda record: record.severity)
        return tuple(_breach(record, as_of) for record in ordered)

    def on_call_load(self, history: IncidentHistory) -> tuple[OnCallLoad, ...]:
        loads = [_load(name, group) for name, group in history.by_responder().items()]
        return tuple(sorted(loads, key=lambda load: (-load.incidents, -load.high_severity)))


def _breach(record: IncidentRecord, as_of: datetime) -> SlaBreach:
    targets = tuple(
        target
        for target, outcome in (
            ("acknowledge", record.acknowledgement_outcome()),
            ("resolve", record.resolution_outcome(as_of)),
        )
        if outcome is SlaOutcome.BREACHED
    )
    return SlaBreach(
        number=record.number,
        title=record.title,
        service=record.service,
        severity=record.severity,
        status=record.status,
        assignee=record.assignee,
        breached_targets=targets,
        resolve_overrun_minutes=_overrun_minutes(record, as_of),
    )


def _overrun_minutes(record: IncidentRecord, as_of: datetime) -> float | None:
    if record.resolution_outcome(as_of) is not SlaOutcome.BREACHED:
        return None
    seconds = record.resolution_overrun(as_of).total_seconds()
    return round(seconds / SECONDS_PER_MINUTE, MINUTES_DIGITS)


def _load(name: str, history: IncidentHistory) -> OnCallLoad:
    return OnCallLoad(
        assignee=name,
        incidents=len(history),
        high_severity=history.count(lambda record: record.severity.is_high),
        off_hours=history.count(lambda record: record.created_off_hours()),
        escalated=history.count(lambda record: record.is_escalated),
    )


def _slowest_resolution_first(group: KpiGroup) -> float:
    return -(group.summary.time_to_resolve.median_minutes or 0.0)
