from enum import StrEnum


class IncidentStatus(StrEnum):
    TRIGGERED = "Triggered"
    ACKNOWLEDGED = "Acknowledged"
    MITIGATED = "Mitigated"
    RESOLVED = "Resolved"
