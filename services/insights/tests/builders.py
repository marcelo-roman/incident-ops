from datetime import UTC, datetime, timedelta
from typing import Any
from uuid import UUID, uuid4

from incident_insights.domain.incidents.history import IncidentHistory
from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.incidents.sla import STANDARD_SLA_POLICY, SlaState
from incident_insights.domain.incidents.status import IncidentStatus
from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.reporting_window import ReportingWindow
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.domain.shared.severity import Severity
from incident_insights.infrastructure.incidents.acl import IncidentTranslator

AS_OF = datetime(2026, 9, 28, tzinfo=UTC)


def make_record(
    *,
    number: int = 1001,
    title: str = "Checkout API latency above 1200ms",
    description: str = "Cart requests are slow.",
    service: str = "checkout",
    severity: Severity = Severity.SEV2,
    created_at: datetime = AS_OF - timedelta(days=3),
    ack_after: timedelta | None = timedelta(minutes=10),
    resolve_after: timedelta | None = timedelta(hours=2),
    mitigate_after: timedelta | None = None,
    assignee: str | None = "alice.nguyen",
    escalation_level: int = 1,
    root_cause: str | None = None,
    source: DetectionSource = DetectionSource.MANUAL,
    incident_id: UUID | None = None,
    acknowledgement_breached: bool | None = None,
    sla_state: SlaState = SlaState.MET,
) -> IncidentRecord:
    targets = STANDARD_SLA_POLICY.targets_for(severity)
    return IncidentRecord(
        id=incident_id or uuid4(),
        number=number,
        title=title,
        description=description,
        service=ServiceId(service),
        severity=severity,
        status=_status(ack_after, mitigate_after, resolve_after),
        created_at=created_at,
        acknowledge_due_at=created_at + targets.acknowledge_within,
        resolve_due_at=created_at + targets.resolve_within,
        sla_state=sla_state,
        source=source,
        assignee=assignee,
        escalation_level=escalation_level,
        acknowledged_at=_after(created_at, ack_after),
        mitigated_at=_after(created_at, mitigate_after),
        resolved_at=_after(created_at, resolve_after),
        acknowledgement_breached=_acknowledgement_breached(
            acknowledgement_breached, ack_after, targets.acknowledge_within, escalation_level
        ),
        root_cause=root_cause,
    )


def history_of(*records: IncidentRecord) -> IncidentHistory:
    return IncidentHistory(records)


def window_of(days: int, as_of: datetime = AS_OF) -> ReportingWindow:
    return ReportingWindow.trailing(days, as_of)


def payload_json(record: IncidentRecord) -> dict[str, Any]:
    payload = IncidentTranslator().to_payload(record)
    return payload.model_dump(mode="json", by_alias=True, exclude={"timeline"})


def _acknowledgement_breached(
    explicit: bool | None, ack_after: timedelta | None, window: timedelta, escalation_level: int
) -> bool:
    if explicit is not None:
        return explicit
    if escalation_level > 1:
        return True
    return ack_after is not None and ack_after > window


def _after(created_at: datetime, delta: timedelta | None) -> datetime | None:
    if delta is None:
        return None
    return created_at + delta


def _status(
    ack_after: timedelta | None,
    mitigate_after: timedelta | None,
    resolve_after: timedelta | None,
) -> IncidentStatus:
    if resolve_after is not None:
        return IncidentStatus.RESOLVED
    if mitigate_after is not None:
        return IncidentStatus.MITIGATED
    if ack_after is not None:
        return IncidentStatus.ACKNOWLEDGED
    return IncidentStatus.TRIGGERED
