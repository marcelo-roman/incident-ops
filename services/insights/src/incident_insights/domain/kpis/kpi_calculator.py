from datetime import datetime

import pandas as pd

from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.incidents.sla import SlaOutcome
from incident_insights.domain.kpis.report import (
    DetectionCoverage,
    KpiGroup,
    KpiReport,
    KpiSummary,
    SlaCompliance,
    SourceKpi,
    WeeklyKpi,
)
from incident_insights.domain.shared.duration_stats import DurationStats
from incident_insights.domain.shared.percentage import Percentage
from incident_insights.domain.shared.reporting_window import ReportingWindow

FACT_COLUMNS = ["mtta", "mttr", "acknowledgement", "resolution", "sla"]


class KpiCalculator:
    def report(self, history: IncidentHistory, window: ReportingWindow) -> KpiReport:
        as_of = window.end
        return KpiReport(
            window=window,
            overall=self.summarize(history, as_of),
            detection=self.detection(history),
            by_service=self.by_service(history, as_of),
            by_severity=tuple(
                KpiGroup(self.summarize(group, as_of), severity=severity)
                for severity, group in history.by_severity().items()
            ),
            by_service_severity=tuple(
                KpiGroup(self.summarize(group, as_of), service=service, severity=severity)
                for service, services in history.by_service().items()
                for severity, group in services.by_severity().items()
            ),
            weekly=tuple(
                WeeklyKpi(week, self.summarize(history.in_week(week), as_of))
                for week in window.week_starts()
            ),
        )

    def summarize(self, history: IncidentHistory, as_of: datetime) -> KpiSummary:
        facts = _facts(history, as_of)
        decided = facts[facts["sla"] != SlaOutcome.PENDING]
        return KpiSummary(
            incidents=len(facts),
            time_to_acknowledge=_stats(facts["mtta"]),
            time_to_resolve=_stats(facts["mttr"]),
            sla=SlaCompliance(
                evaluated=len(decided),
                breached=int((decided["sla"] == SlaOutcome.BREACHED).sum()),
                acknowledgement=_met_share(facts["acknowledgement"]),
                resolution=_met_share(facts["resolution"]),
                overall=_met_share(facts["sla"]),
            ),
        )

    def by_service(self, history: IncidentHistory, as_of: datetime) -> tuple[KpiGroup, ...]:
        return tuple(
            KpiGroup(self.summarize(group, as_of), service=service)
            for service, group in history.by_service().items()
        )

    def detection(self, history: IncidentHistory) -> DetectionCoverage:
        total = len(history)
        return DetectionCoverage(
            incidents=total,
            monitoring_share=Percentage.share(
                history.count(lambda record: record.detected_by_monitoring()), total
            ),
            by_source=tuple(
                SourceKpi(
                    source=source,
                    incidents=len(group),
                    share=Percentage.share(len(group), total),
                    time_to_acknowledge=DurationStats.of_minutes(group.acknowledgement_minutes()),
                )
                for source, group in history.by_source().items()
            ),
        )


def _facts(history: IncidentHistory, as_of: datetime) -> pd.DataFrame:
    rows = [
        {
            "mtta": record.minutes_to_acknowledge(),
            "mttr": record.minutes_to_resolve(),
            "acknowledgement": record.acknowledgement_outcome(),
            "resolution": record.resolution_outcome(as_of),
            "sla": record.sla_outcome(as_of),
        }
        for record in history
    ]
    return pd.DataFrame.from_records(rows, columns=FACT_COLUMNS)


def _stats(minutes: pd.Series) -> DurationStats:
    return DurationStats.of_minutes(minutes.dropna().astype(float).tolist())


def _met_share(outcomes: pd.Series) -> Percentage | None:
    decided = outcomes[outcomes != SlaOutcome.PENDING]
    if decided.empty:
        return None
    return Percentage.from_fraction(float((decided == SlaOutcome.MET).mean()))
