from uuid import UUID

from pydantic import BaseModel, ConfigDict
from pydantic.alias_generators import to_camel


class Schema(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, frozen=True)


class RcaDraftRequest(Schema):
    incident_id: UUID


class HealthStatus(Schema):
    status: str
