from datetime import datetime
from typing import Self

from incident_insights.application.outputs.common import Output, WindowOutput
from incident_insights.domain.recurring.cluster import Cluster, RecurringReport


class ServiceCountOutput(Output):
    service_id: str
    count: int


class RecurringClusterOutput(Output):
    label: str
    terms: list[str]
    count: int
    cohesion: float
    services: list[ServiceCountOutput]
    sample_titles: list[str]
    first_seen: datetime
    last_seen: datetime

    @classmethod
    def of(cls, cluster: Cluster) -> Self:
        return cls(
            label=cluster.label,
            terms=list(cluster.terms),
            count=cluster.count,
            cohesion=cluster.cohesion,
            services=[
                ServiceCountOutput(service_id=str(item.service), count=item.count)
                for item in cluster.services
            ],
            sample_titles=list(cluster.sample_titles),
            first_seen=cluster.first_seen,
            last_seen=cluster.last_seen,
        )


class RecurringReportOutput(Output):
    window: WindowOutput
    incidents_analyzed: int
    clusters_evaluated: int
    silhouette: float | None
    clusters: list[RecurringClusterOutput]

    @classmethod
    def of(cls, report: RecurringReport) -> Self:
        return cls(
            window=WindowOutput.of(report.window),
            incidents_analyzed=report.incidents_analyzed,
            clusters_evaluated=report.clusters_evaluated,
            silhouette=report.silhouette,
            clusters=[RecurringClusterOutput.of(cluster) for cluster in report.clusters],
        )
