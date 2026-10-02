from uuid import UUID

from incident_insights.application.outputs.rca import RcaDraftOutput
from incident_insights.application.ports import Clock, IncidentSource, RcaDrafter


class IncidentNotFoundError(Exception):
    def __init__(self, incident_id: UUID) -> None:
        super().__init__(f"Incident {incident_id} was not found")
        self.incident_id = incident_id


class DraftRca:
    def __init__(self, source: IncidentSource, drafter: RcaDrafter, clock: Clock) -> None:
        self._source = source
        self._drafter = drafter
        self._clock = clock

    def execute(self, incident_id: UUID) -> RcaDraftOutput:
        incident = self._source.get(incident_id)
        if incident is None:
            raise IncidentNotFoundError(incident_id)
        draft = self._drafter.draft(incident)
        return RcaDraftOutput.of(draft, incident, self._drafter.name, self._clock())
