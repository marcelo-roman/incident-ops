from dataclasses import fields
from datetime import timedelta
from uuid import uuid4

from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.incidents.timeline import TimelineEntry, TimelineKind
from incident_insights.domain.shared.detection_source import DetectionSource
from incident_insights.domain.shared.service_id import ServiceId
from incident_insights.infrastructure.incidents.acl import (
    IncidentDetailPayload,
    IncidentPayload,
    IncidentTranslator,
)
from tests.builders import make_record, payload_json

TRANSLATOR = IncidentTranslator()


def test_export_payload_is_translated_into_domain_terms() -> None:
    payload = IncidentPayload.model_validate(
        {
            **payload_json(make_record()),
            "serviceId": "payments-gateway",
            "ackDueAt": "2026-09-25T00:30:00Z",
            "acknowledgementBreached": True,
            "source": "Alertmanager",
            "alertFingerprint": "abc123",
            "unknownField": "ignored",
        }
    )

    record = TRANSLATOR.to_record(payload)

    assert record.service == ServiceId("payments-gateway")
    assert record.acknowledge_due_at.isoformat() == "2026-09-25T00:30:00+00:00"
    assert record.acknowledgement_breached
    assert record.source is DetectionSource.ALERTMANAGER
    assert record.alert_fingerprint == "abc123"
    assert record.timeline == ()


def test_api_field_names_do_not_leak_into_the_domain() -> None:
    names = {field.name for field in fields(IncidentRecord)}

    assert not names & {"service_id", "ack_due_at", "serviceId", "ackDueAt"}


def test_detail_payload_carries_the_timeline() -> None:
    record = make_record()
    payload = IncidentDetailPayload.model_validate(
        {
            **payload_json(record),
            "timeline": [
                {
                    "id": str(uuid4()),
                    "incidentId": str(record.id),
                    "at": "2026-09-25T01:00:00Z",
                    "kind": "Escalated",
                    "actor": "system",
                    "message": "Escalated to level 2",
                }
            ],
        }
    )

    translated = TRANSLATOR.to_record(payload)

    assert translated.timeline[0].kind is TimelineKind.ESCALATED


def test_round_trip_preserves_the_record() -> None:
    record = make_record(mitigate_after=timedelta(minutes=30), root_cause="Index dropped")
    entry = TimelineEntry(uuid4(), record.created_at, TimelineKind.NOTE, "alice", "Paged")
    with_timeline = IncidentRecord(
        **{field.name: getattr(record, field.name) for field in fields(record)}
        | {"timeline": (entry,)}
    )

    payload = TRANSLATOR.to_payload(with_timeline)
    restored = TRANSLATOR.to_record(payload)

    assert payload.service_id == "checkout"
    assert payload.timeline[0].incident_id == record.id
    assert all(
        getattr(restored, field.name) == getattr(with_timeline, field.name)
        for field in fields(record)
    )
