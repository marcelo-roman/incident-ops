from datetime import UTC, datetime, timedelta

import pytest

from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.recurring.cluster import Cluster, RecurringReport
from incident_insights.domain.recurring.recurring_issue_detector import (
    RecurringIssueDetector,
    distinct_terms,
)
from incident_insights.domain.recurring.text_features import ClusteringOptions
from incident_insights.domain.shared.service_id import ServiceId
from tests.builders import AS_OF, history_of, make_record, window_of

WINDOW = window_of(60)
DETECTOR = RecurringIssueDetector()

FAMILIES = (
    ("Database connection pool exhausted", "Requests fail with connection pool timeout errors."),
    ("Payment webhook delivery backlog", "Payment status webhooks are queued and delayed."),
    ("Search index lag after catalog update", "Search shows outdated prices; indexer is stuck."),
)


def _history() -> IncidentHistory:
    services = ("checkout", "payments-gateway", "search")
    return history_of(
        *(
            make_record(
                number=index,
                title=FAMILIES[index % 3][0],
                description=FAMILIES[index % 3][1],
                service=services[index % 3],
                created_at=AS_OF - timedelta(days=index + 1),
            )
            for index in range(18)
        )
    )


def test_groups_similar_incidents_into_labelled_clusters() -> None:
    report = DETECTOR.detect(_history(), WINDOW)

    assert report.clusters_evaluated == 3
    assert [cluster.count for cluster in report.clusters] == [6, 6, 6]
    labels = " ".join(cluster.label for cluster in report.clusters)
    assert "connection pool" in labels
    assert "webhook" in labels


def test_cluster_lists_services_samples_and_dates() -> None:
    report = DETECTOR.detect(_history(), WINDOW)
    pool = next(cluster for cluster in report.clusters if "pool" in cluster.label)

    assert pool.services[0].service == ServiceId("checkout")
    assert pool.sample_titles == ("Database connection pool exhausted",)
    assert pool.first_seen < pool.last_seen
    assert 0 < pool.cohesion <= 1
    assert report.top(1) == report.clusters[:1]


def test_detection_is_deterministic() -> None:
    assert DETECTOR.detect(_history(), WINDOW) == DETECTOR.detect(_history(), WINDOW)


def test_small_clusters_are_dropped() -> None:
    history = history_of(*_history(), make_record(title="Unrelated cosmic ray bit flip"))
    detector = RecurringIssueDetector(ClusteringOptions(min_cluster_size=3, max_clusters=4))

    report = detector.detect(history, WINDOW)

    assert all(cluster.is_recurring(3) for cluster in report.clusters)


def test_too_few_incidents_return_an_empty_report() -> None:
    report = DETECTOR.detect(history_of(*list(_history())[:2]), WINDOW)

    assert report == RecurringReport.empty(WINDOW, 2)


def test_identical_documents_cannot_be_clustered() -> None:
    report = DETECTOR.detect(history_of(*(make_record(number=i) for i in range(5))), WINDOW)

    assert report.clusters == ()


def test_distinct_terms_prefers_phrases_and_skips_covered_words() -> None:
    ranked = ["pool", "connection pool", "connection", "timeout", "pool timeout", "errors"]

    assert distinct_terms(ranked, 3) == ("connection pool", "pool timeout", "errors")


def test_cluster_invariants() -> None:
    moment = datetime(2026, 9, 1, tzinfo=UTC)
    with pytest.raises(ValueError, match="term"):
        Cluster((), 1, 1.0, (), (), moment, moment)
    with pytest.raises(ValueError, match="at least one incident"):
        Cluster(("a",), 0, 1.0, (), (), moment, moment)
    with pytest.raises(ValueError, match="first occurrence"):
        Cluster(("a",), 1, 1.0, (), (), moment, moment - timedelta(days=1))
