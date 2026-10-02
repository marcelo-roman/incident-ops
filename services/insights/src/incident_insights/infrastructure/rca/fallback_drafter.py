from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.rca.draft import RcaDraft
from incident_insights.domain.rca.rca_draft_composer import RcaDraftComposer

DRAFTER_NAME = "deterministic-fallback"


class DeterministicRcaDrafter:
    def __init__(self, composer: RcaDraftComposer) -> None:
        self._composer = composer

    @property
    def name(self) -> str:
        return DRAFTER_NAME

    def draft(self, incident: IncidentRecord) -> RcaDraft:
        return self._composer.compose(incident)
