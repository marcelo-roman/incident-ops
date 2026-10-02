from datetime import datetime
from typing import Self
from uuid import UUID

from incident_insights.application.outputs.common import Output
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.rca.draft import RcaDraft


class RcaTimelineEventOutput(Output):
    at: str
    event: str


class ActionItemOutput(Output):
    title: str
    owner: str
    priority: str


class RcaDraftOutput(Output):
    summary: str
    impact: str
    timeline: list[RcaTimelineEventOutput]
    contributing_factors: list[str]
    action_items: list[ActionItemOutput]
    incident_id: UUID
    incident_number: int
    generated_by: str
    generated_at: datetime

    @classmethod
    def of(
        cls, draft: RcaDraft, incident: IncidentRecord, generated_by: str, generated_at: datetime
    ) -> Self:
        return cls(
            summary=draft.summary,
            impact=draft.impact,
            timeline=[
                RcaTimelineEventOutput(at=event.at, event=event.event) for event in draft.timeline
            ],
            contributing_factors=list(draft.contributing_factors),
            action_items=[
                ActionItemOutput(title=item.title, owner=item.owner, priority=item.priority.value)
                for item in draft.action_items
            ],
            incident_id=incident.id,
            incident_number=incident.number,
            generated_by=generated_by,
            generated_at=generated_at,
        )
