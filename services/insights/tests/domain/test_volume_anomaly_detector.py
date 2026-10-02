from datetime import date, timedelta

import numpy as np
import pytest

from incident_insights.domain.anomalies.volume_anomaly import VolumeAnomaly
from incident_insights.domain.anomalies.volume_anomaly_detector import (
    AnomalyOptions,
    RollingBaseline,
    VolumeAnomalyDetector,
)
from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.shared.service_id import ServiceId
from tests.builders import AS_OF, history_of, make_record, window_of

WINDOW = window_of(84)
DETECTOR = VolumeAnomalyDetector()


def _weekly(counts: list[int]) -> IncidentHistory:
    first_monday = AS_OF - timedelta(weeks=len(counts) - 1)
    return history_of(
        *(
            make_record(created_at=first_monday + timedelta(weeks=week, hours=hour))
            for week, count in enumerate(counts)
            for hour in range(count)
        )
    )


def test_rolling_baseline_uses_only_previous_weeks() -> None:
    options = AnomalyOptions(baseline_weeks=3, min_baseline_weeks=2)

    baseline = RollingBaseline.of(np.array([2, 4, 6, 20]), options)

    assert np.isnan(baseline.z_scores[:2]).all()
    assert baseline.means[2] == 3
    assert baseline.means[3] == 4
    assert round(float(baseline.stds[3]), 3) == 1.633
    assert round(float(baseline.z_scores[3]), 2) == 9.8


def test_flat_baseline_uses_minimum_deviation() -> None:
    baseline = RollingBaseline.of(np.array([1, 1, 1, 1, 5]), AnomalyOptions())

    assert baseline.z_scores[4] == 4


def test_detects_spike_week_for_service() -> None:
    report = DETECTOR.detect(_weekly([2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 12, 2]), WINDOW)

    assert len(report.anomalies) == 1
    anomaly = report.anomalies[0]
    assert anomaly.service == ServiceId("checkout")
    assert anomaly.count == 12
    assert anomaly.week_start == date(2026, 9, 21)
    assert anomaly.z_score > report.threshold
    assert report.method == "rolling-z-score"


def test_ignores_small_absolute_counts() -> None:
    report = DETECTOR.detect(_weekly([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0]), WINDOW)

    assert report.anomalies == ()


def test_empty_history_has_no_anomalies() -> None:
    assert DETECTOR.detect(history_of(), WINDOW).anomalies == ()


def test_options_and_anomaly_invariants() -> None:
    with pytest.raises(ValueError, match="baseline"):
        AnomalyOptions(baseline_weeks=2, min_baseline_weeks=3)
    with pytest.raises(ValueError, match="negative"):
        VolumeAnomaly(ServiceId("search"), date(2026, 9, 21), -1, 0.0, 0.0, 0.0)
