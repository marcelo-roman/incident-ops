from datetime import date, timedelta

import pytest

from incident_insights.domain.incidents.history import IncidentHistory, WeeklyVolume
from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.domain.shared.severity import Severity
from tests.builders import AS_OF, history_of, make_record, window_of


def _history() -> IncidentHistory:
    return history_of(
        make_record(number=1, service="search", severity=Severity.SEV3, ack_after=None),
        make_record(number=2, service="checkout", created_at=AS_OF - timedelta(days=10)),
        make_record(number=3, service="checkout", severity=Severity.SEV1, assignee=None),
        make_record(number=4, service="search", source=DetectionSource.AZURE_MONITOR),
    )


def test_within_keeps_records_created_in_the_window() -> None:
    history = _history().within(window_of(7))

    assert [record.number for record in history] == [1, 3, 4]
    assert len(history) == 3
    assert not history.is_empty
    assert IncidentHistory().is_empty


def test_groups_are_sorted_and_keep_record_order() -> None:
    by_service = _history().by_service()

    assert list(by_service) == [ServiceId("checkout"), ServiceId("search")]
    assert [record.number for record in by_service[ServiceId("search")]] == [1, 4]
    assert list(_history().by_severity()) == [Severity.SEV1, Severity.SEV2, Severity.SEV3]
    assert list(_history().by_responder()) == ["alice.nguyen", "unassigned"]


def test_by_source_lists_every_source() -> None:
    by_source = _history().by_source()

    assert list(by_source) == list(DetectionSource)
    assert len(by_source[DetectionSource.ALERTMANAGER]) == 0
    assert len(by_source[DetectionSource.AZURE_MONITOR]) == 1


def test_weekly_volume_counts_each_service_per_week() -> None:
    volume = _history().weekly_volume(window_of(14))

    assert volume.weeks == (date(2026, 9, 14), date(2026, 9, 21), date(2026, 9, 28))
    assert volume.counts[ServiceId("checkout")] == (1, 1, 0)
    assert volume.counts[ServiceId("search")] == (0, 2, 0)


def test_weekly_volume_invariant() -> None:
    with pytest.raises(ValueError, match="one count per week"):
        WeeklyVolume(weeks=(date(2026, 9, 21),), counts={ServiceId("search"): (1, 2)})


def test_find_count_and_minutes() -> None:
    history = _history()
    first = next(iter(history))

    assert history.find(first.id) is first
    assert history.find(make_record().id) is None
    assert history.count(lambda record: record.severity.is_high) == 3
    assert history.acknowledgement_minutes() == [10.0, 10.0, 10.0]
    assert history.in_week(date(2026, 9, 14)) == history_of(*list(history)[1:2])


def test_histories_compare_by_records() -> None:
    records = list(_history())

    assert IncidentHistory(records) == IncidentHistory(records)
    assert hash(IncidentHistory(records)) == hash(IncidentHistory(records))
    assert IncidentHistory(records).__eq__(records) is NotImplemented
