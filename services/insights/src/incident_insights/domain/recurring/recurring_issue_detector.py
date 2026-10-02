import re
from collections import Counter

import numpy as np

from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.recurring.cluster import Cluster, RecurringReport, ServiceCount
from incident_insights.domain.recurring.text_features import (
    Clustering,
    ClusteringOptions,
    TextFeatures,
    choose_clustering,
    extract_features,
)
from incident_insights.domain.shared.reporting_window import ReportingWindow

LABEL_CANDIDATES = 12
SILHOUETTE_DIGITS = 3
COHESION_DIGITS = 3
WORD = re.compile(r"\w+")


class RecurringIssueDetector:
    def __init__(self, options: ClusteringOptions | None = None) -> None:
        self._options = options or ClusteringOptions()

    def detect(self, history: IncidentHistory, window: ReportingWindow) -> RecurringReport:
        records = tuple(history)
        documents = [f"{record.title}. {record.description}" for record in records]
        features = extract_features(documents, self._options.random_state)
        if features is None:
            return RecurringReport.empty(window, len(records))
        clustering = choose_clustering(features.embedding, self._options)
        if clustering is None:
            return RecurringReport.empty(window, len(records))
        return RecurringReport(
            window=window,
            incidents_analyzed=len(records),
            clusters_evaluated=clustering.clusters,
            silhouette=round(clustering.silhouette, SILHOUETTE_DIGITS),
            clusters=self._recurring_clusters(records, features, clustering),
        )

    def _recurring_clusters(
        self,
        records: tuple[IncidentRecord, ...],
        features: TextFeatures,
        clustering: Clustering,
    ) -> tuple[Cluster, ...]:
        clusters = [
            self._describe(records, features, np.flatnonzero(clustering.labels == label))
            for label in range(clustering.clusters)
        ]
        recurring = [item for item in clusters if item.is_recurring(self._options.min_cluster_size)]
        return tuple(sorted(recurring, key=lambda item: (-item.count, item.label)))

    def _describe(
        self, records: tuple[IncidentRecord, ...], features: TextFeatures, members: np.ndarray
    ) -> Cluster:
        rows = features.tfidf[members]
        centroid = np.asarray(rows.mean(axis=0)).ravel()
        norm = max(float(np.linalg.norm(centroid)), 1e-12)
        similarity = np.asarray(rows @ centroid).ravel() / norm
        subset = [records[int(index)] for index in members]
        return Cluster(
            terms=self._label_terms(features.terms, centroid, subset),
            count=len(subset),
            cohesion=round(float(similarity.mean()), COHESION_DIGITS),
            services=_services(subset),
            sample_titles=self._central_titles(subset, similarity),
            first_seen=min(record.created_at for record in subset),
            last_seen=max(record.created_at for record in subset),
        )

    def _label_terms(
        self, terms: np.ndarray, centroid: np.ndarray, subset: list[IncidentRecord]
    ) -> tuple[str, ...]:
        order = np.argsort(-centroid, kind="stable")[:LABEL_CANDIDATES]
        ranked = [str(term) for term in terms[order]]
        title_words = {word for record in subset for word in WORD.findall(record.title.lower())}
        in_titles = [term for term in ranked if set(term.split()) <= title_words]
        return distinct_terms(in_titles + ranked, self._options.label_terms)

    def _central_titles(
        self, subset: list[IncidentRecord], similarity: np.ndarray
    ) -> tuple[str, ...]:
        order = np.argsort(-similarity, kind="stable")
        titles = dict.fromkeys(subset[int(index)].title for index in order)
        return tuple(list(titles)[: self._options.sample_titles])


def distinct_terms(ranked: list[str], limit: int) -> tuple[str, ...]:
    phrases = [term for term in ranked if " " in term]
    chosen: list[str] = []
    covered: set[str] = set()
    for term in ranked:
        words = set(term.split())
        if words <= covered or _inside_phrase(term, phrases):
            continue
        chosen.append(term)
        covered |= words
        if len(chosen) == limit:
            break
    return tuple(chosen)


def _inside_phrase(term: str, phrases: list[str]) -> bool:
    return " " not in term and any(term in phrase.split() for phrase in phrases)


def _services(subset: list[IncidentRecord]) -> tuple[ServiceCount, ...]:
    counts = Counter(record.service for record in subset)
    by_name = sorted(counts.items(), key=lambda item: item[0])
    ordered = sorted(by_name, key=lambda item: -item[1])
    return tuple(ServiceCount(service, count) for service, count in ordered)
