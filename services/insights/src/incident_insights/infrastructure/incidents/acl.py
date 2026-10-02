from uuid import UUID

from pydantic import AwareDatetime, BaseModel, ConfigDict
from pydantic.alias_generators import to_camel

from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.incidents.sla import SlaState
from incident_insights.domain.incidents.status import IncidentStatus
from incident_insights.domain.incidents.timeline import TimelineEntry, TimelineKind
from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.domain.shared.severity import Severity


class ApiPayload(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="ignore")


class TimelineEntryPayload(ApiPayload):
    id: UUID
    incident_id: UUID
    at: AwareDatetime
    kind: TimelineKind
    actor: str
    message: str


class IncidentPayload(ApiPayload):
    id: UUID
    number: int
    title: str
    description: str
    service_id: str
    severity: Severity
    status: IncidentStatus
    assignee: str | None = None
    escalation_level: int = 1
    created_at: AwareDatetime
    acknowledged_at: AwareDatetime | None = None
    mitigated_at: AwareDatetime | None = None
    resolved_at: AwareDatetime | None = None
    ack_due_at: AwareDatetime
    resolve_due_at: AwareDatetime
    acknowledgement_breached: bool = False
    sla_state: SlaState
    root_cause: str | None = None
    source: DetectionSource = DetectionSource.MANUAL
    alert_fingerprint: str | None = None


class IncidentDetailPayload(IncidentPayload):
    timeline: list[TimelineEntryPayload] = []


class IncidentTranslator:
    def to_record(self, payload: IncidentPayload) -> IncidentRecord:
        return IncidentRecord(
            id=payload.id,
            number=payload.number,
            title=payload.title,
            description=payload.description,
            service=ServiceId(payload.service_id),
            severity=payload.severity,
            status=payload.status,
            created_at=payload.created_at,
            acknowledge_due_at=payload.ack_due_at,
            resolve_due_at=payload.resolve_due_at,
            sla_state=payload.sla_state,
            source=payload.source,
            assignee=payload.assignee,
            escalation_level=payload.escalation_level,
            acknowledged_at=payload.acknowledged_at,
            mitigated_at=payload.mitigated_at,
            resolved_at=payload.resolved_at,
            acknowledgement_breached=payload.acknowledgement_breached,
            root_cause=payload.root_cause,
            alert_fingerprint=payload.alert_fingerprint,
            timeline=self._timeline(payload),
        )

    def to_payload(self, record: IncidentRecord) -> IncidentDetailPayload:
        return IncidentDetailPayload(
            id=record.id,
            number=record.number,
            title=record.title,
            description=record.description,
            service_id=str(record.service),
            severity=record.severity,
            status=record.status,
            assignee=record.assignee,
            escalation_level=record.escalation_level,
            created_at=record.created_at,
            acknowledged_at=record.acknowledged_at,
            mitigated_at=record.mitigated_at,
            resolved_at=record.resolved_at,
            ack_due_at=record.acknowledge_due_at,
            resolve_due_at=record.resolve_due_at,
            acknowledgement_breached=record.acknowledgement_breached,
            sla_state=record.sla_state,
            root_cause=record.root_cause,
            source=record.source,
            alert_fingerprint=record.alert_fingerprint,
            timeline=[_entry_payload(record.id, entry) for entry in record.timeline],
        )

    def _timeline(self, payload: IncidentPayload) -> tuple[TimelineEntry, ...]:
        if not isinstance(payload, IncidentDetailPayload):
            return ()
        return tuple(
            TimelineEntry(
                id=entry.id, at=entry.at, kind=entry.kind, actor=entry.actor, message=entry.message
            )
            for entry in payload.timeline
        )


def _entry_payload(incident_id: UUID, entry: TimelineEntry) -> TimelineEntryPayload:
    return TimelineEntryPayload(
        id=entry.id,
        incident_id=incident_id,
        at=entry.at,
        kind=entry.kind,
        actor=entry.actor,
        message=entry.message,
    )
