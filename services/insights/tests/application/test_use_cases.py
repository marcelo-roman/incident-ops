from datetime import timedelta
from uuid import uuid4

import pytest

from incident_insights.application.build_ktlo_report import BuildKtloReport
from incident_insights.application.clock import fixed_clock
from incident_insights.application.detect_volume_anomalies import DetectVolumeAnomalies
from incident_insights.application.draft_rca import DraftRca, IncidentNotFoundError
from incident_insights.application.find_recurring_issues import FindRecurringIssues
from incident_insights.application.get_kpis import GetKpis
from incident_insights.application.history_loader import HistoryLoader
from incident_insights.application.ports import IncidentSourceError, RcaDraftError
from incident_insights.domain.anomalies.volume_anomaly_detector import VolumeAnomalyDetector
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.kpis.kpi_calculator import KpiCalculator
from incident_insights.domain.rca.draft import ActionItem, Priority, RcaDraft
from incident_insights.domain.recurring.recurring_issue_detector import RecurringIssueDetector
from incident_insights.domain.reports.ktlo_report_composer import KtloReportComposer
from tests.builders import AS_OF, make_record, window_of
from tests.fakes import (
    CannedRcaDrafter,
    FailingRcaDrafter,
    InMemoryIncidentSource,
    UnavailableIncidentSource,
)

CLOCK = fixed_clock(AS_OF)
DRAFT = RcaDraft(
    summary="Checkout latency",
    impact="Slow orders",
    timeline=(),
    contributing_factors=("Index dropped",),
    action_items=(ActionItem("Restore index", "checkout owners", Priority.P1),),
)


def _records() -> list[IncidentRecord]:
    return [
        make_record(number=index, created_at=AS_OF - timedelta(days=index * 5 + 1))
        for index in range(8)
    ]


def _loader(source: InMemoryIncidentSource) -> HistoryLoader:
    return HistoryLoader(source, CLOCK)


def test_loader_requests_the_trailing_window_from_the_source() -> None:
    source = InMemoryIncidentSource(_records())

    history, window = _loader(source).trailing(14)

    assert source.export_calls == [window_of(14)]
    assert window.days == 14
    assert len(history) == 3


def test_get_kpis_returns_the_camel_case_output() -> None:
    output = GetKpis(_loader(InMemoryIncidentSource(_records())), KpiCalculator()).execute(20)

    body = output.model_dump(by_alias=True)
    assert body["window"]["end"] == AS_OF
    assert body["overall"]["incidents"] == 4
    assert body["overall"]["sla"]["overallPct"] == 100.0
    assert body["bySeverity"][0]["severity"] == "Sev2"


def test_find_recurring_and_detect_anomalies_share_the_window() -> None:
    loader = _loader(InMemoryIncidentSource(_records()))

    recurring = FindRecurringIssues(loader, RecurringIssueDetector()).execute(60)
    anomalies = DetectVolumeAnomalies(loader, VolumeAnomalyDetector()).execute(60)

    assert recurring.incidents_analyzed == 8
    assert recurring.window == anomalies.window
    assert anomalies.method == "rolling-z-score"


def test_build_report_is_stamped_with_the_window_end() -> None:
    composer = KtloReportComposer(KpiCalculator(), RecurringIssueDetector())

    report = BuildKtloReport(_loader(InMemoryIncidentSource(_records())), composer).execute(30)

    assert report.generated_at == AS_OF
    assert report.overall.incidents == 6


def test_source_failures_propagate() -> None:
    loader = HistoryLoader(UnavailableIncidentSource(), CLOCK)

    with pytest.raises(IncidentSourceError):
        GetKpis(loader, KpiCalculator()).execute(30)


def test_draft_rca_sends_the_record_and_labels_the_output() -> None:
    record = make_record(number=1042)
    drafter = CannedRcaDrafter(DRAFT)

    output = DraftRca(InMemoryIncidentSource([record]), drafter, CLOCK).execute(record.id)

    assert drafter.received == [record]
    assert output.generated_by == "canned"
    assert output.incident_number == 1042
    assert output.generated_at == AS_OF
    assert output.action_items[0].priority == "P1"


def test_draft_rca_for_unknown_incident() -> None:
    use_case = DraftRca(InMemoryIncidentSource([]), CannedRcaDrafter(DRAFT), CLOCK)

    with pytest.raises(IncidentNotFoundError):
        use_case.execute(uuid4())


def test_draft_rca_propagates_drafter_failures() -> None:
    record = make_record()
    use_case = DraftRca(InMemoryIncidentSource([record]), FailingRcaDrafter(), CLOCK)

    with pytest.raises(RcaDraftError):
        use_case.execute(record.id)
