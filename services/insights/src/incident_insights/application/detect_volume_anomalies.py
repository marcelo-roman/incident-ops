from incident_insights.application.history_loader import HistoryLoader
from incident_insights.application.outputs.anomalies import AnomalyReportOutput
from incident_insights.domain.anomalies.volume_anomaly_detector import VolumeAnomalyDetector


class DetectVolumeAnomalies:
    def __init__(self, loader: HistoryLoader, detector: VolumeAnomalyDetector) -> None:
        self._loader = loader
        self._detector = detector

    def execute(self, days: int) -> AnomalyReportOutput:
        history, window = self._loader.trailing(days)
        return AnomalyReportOutput.of(self._detector.detect(history, window))
