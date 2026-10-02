from enum import StrEnum


class DetectionSource(StrEnum):
    MANUAL = "Manual"
    ALERTMANAGER = "Alertmanager"
    AZURE_MONITOR = "AzureMonitor"

    @property
    def is_monitoring(self) -> bool:
        return self is not DetectionSource.MANUAL
