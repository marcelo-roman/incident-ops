from enum import StrEnum


class Severity(StrEnum):
    SEV1 = "Sev1"
    SEV2 = "Sev2"
    SEV3 = "Sev3"
    SEV4 = "Sev4"

    @property
    def is_high(self) -> bool:
        return self in (Severity.SEV1, Severity.SEV2)
