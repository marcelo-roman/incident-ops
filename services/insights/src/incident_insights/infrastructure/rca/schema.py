from typing import Literal

from pydantic import BaseModel, ConfigDict
from pydantic.alias_generators import to_camel

from incident_insights.domain.rca.draft import ActionItem, Priority, RcaDraft, RcaTimelineEvent


class SchemaModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


class TimelineEventSchema(SchemaModel):
    at: str
    event: str


class ActionItemSchema(SchemaModel):
    title: str
    owner: str
    priority: Literal["P1", "P2", "P3"]


class RcaDraftSchema(SchemaModel):
    summary: str
    impact: str
    timeline: list[TimelineEventSchema]
    contributing_factors: list[str]
    action_items: list[ActionItemSchema]

    def to_domain(self) -> RcaDraft:
        return RcaDraft(
            summary=self.summary,
            impact=self.impact,
            timeline=tuple(RcaTimelineEvent(item.at, item.event) for item in self.timeline),
            contributing_factors=tuple(self.contributing_factors),
            action_items=tuple(
                ActionItem(item.title, item.owner, Priority(item.priority))
                for item in self.action_items
            ),
        )
