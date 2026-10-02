from dataclasses import dataclass
from datetime import date

import numpy as np
from numpy.lib.stride_tricks import sliding_window_view

from incident_insights.domain.anomalies.volume_anomaly import AnomalyReport, VolumeAnomaly
from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.shared.reporting_window import ReportingWindow
from incident_insights.domain.shared.service_id import ServiceId

METHOD = "rolling-z-score"
DIGITS = 2


@dataclass(frozen=True, slots=True)
class AnomalyOptions:
    baseline_weeks: int = 8
    min_baseline_weeks: int = 4
    threshold: float = 3.0
    min_std: float = 1.0
    min_count: int = 3

    def __post_init__(self) -> None:
        if not 1 <= self.min_baseline_weeks <= self.baseline_weeks:
            raise ValueError("the minimum baseline must fit in the baseline")


@dataclass(frozen=True, slots=True)
class RollingBaseline:
    means: np.ndarray
    stds: np.ndarray
    z_scores: np.ndarray

    @classmethod
    def of(cls, counts: np.ndarray, options: AnomalyOptions) -> "RollingBaseline":
        values = counts.astype(float)
        padded = np.concatenate([np.full(options.baseline_weeks, np.nan), values])
        history = sliding_window_view(padded, options.baseline_weeks)[: values.size]
        present = ~np.isnan(history)
        observed = present.sum(axis=1)
        filled = np.where(present, history, 0.0)
        divisor = np.maximum(observed, 1)
        means = filled.sum(axis=1) / divisor
        deviations = np.where(present, history - means[:, None], 0.0)
        stds = np.sqrt((deviations**2).sum(axis=1) / divisor)
        enough = observed >= options.min_baseline_weeks
        z_scores = (values - means) / np.maximum(stds, options.min_std)
        return cls(
            means=np.where(enough, means, np.nan),
            stds=np.where(enough, stds, np.nan),
            z_scores=np.where(enough, z_scores, np.nan),
        )


class VolumeAnomalyDetector:
    def __init__(self, options: AnomalyOptions | None = None) -> None:
        self._options = options or AnomalyOptions()

    def detect(self, history: IncidentHistory, window: ReportingWindow) -> AnomalyReport:
        volume = history.weekly_volume(window)
        anomalies = [
            anomaly
            for service, counts in volume.counts.items()
            for anomaly in self._service_anomalies(service, volume.weeks, np.asarray(counts))
        ]
        return AnomalyReport(
            window=window,
            method=METHOD,
            baseline_weeks=self._options.baseline_weeks,
            threshold=self._options.threshold,
            anomalies=tuple(sorted(anomalies, key=lambda item: (item.week_start, item.service))),
        )

    def _service_anomalies(
        self, service: ServiceId, weeks: tuple[date, ...], counts: np.ndarray
    ) -> list[VolumeAnomaly]:
        baseline = RollingBaseline.of(counts, self._options)
        flagged = (baseline.z_scores >= self._options.threshold) & (
            counts >= self._options.min_count
        )
        return [
            VolumeAnomaly(
                service=service,
                week_start=weeks[int(position)],
                count=int(counts[position]),
                baseline_mean=round(float(baseline.means[position]), DIGITS),
                baseline_std=round(float(baseline.stds[position]), DIGITS),
                z_score=round(float(baseline.z_scores[position]), DIGITS),
            )
            for position in np.flatnonzero(flagged)
        ]
