from datetime import date
from typing import Self

from incident_insights.application.outputs.common import Output, WindowOutput
from incident_insights.domain.anomalies.volume_anomaly import AnomalyReport, VolumeAnomaly


class VolumeAnomalyOutput(Output):
    service_id: str
    week_start: date
    count: int
    baseline_mean: float
    baseline_std: float
    z_score: float

    @classmethod
    def of(cls, anomaly: VolumeAnomaly) -> Self:
        return cls(
            service_id=str(anomaly.service),
            week_start=anomaly.week_start,
            count=anomaly.count,
            baseline_mean=anomaly.baseline_mean,
            baseline_std=anomaly.baseline_std,
            z_score=anomaly.z_score,
        )


class AnomalyReportOutput(Output):
    window: WindowOutput
    method: str
    baseline_weeks: int
    threshold: float
    anomalies: list[VolumeAnomalyOutput]

    @classmethod
    def of(cls, report: AnomalyReport) -> Self:
        return cls(
            window=WindowOutput.of(report.window),
            method=report.method,
            baseline_weeks=report.baseline_weeks,
            threshold=report.threshold,
            anomalies=[VolumeAnomalyOutput.of(item) for item in report.anomalies],
        )
