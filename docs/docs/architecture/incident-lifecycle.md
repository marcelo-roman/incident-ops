# Incident lifecycle

How an incident moves through its states, what each transition writes, and which events and messages it produces.

## State machine

```mermaid
stateDiagram-v2
    direction LR
    [*] --> Triggered: POST /api/incidents<br/>or alert firing
    Triggered --> Triggered: escalate<br/>(level + 1, max 3)
    Triggered --> Acknowledged: acknowledge
    Triggered --> Mitigated: alert resolved<br/>(actor alerting)
    Triggered --> Resolved: resolve
    Acknowledged --> Mitigated: mitigate<br/>or alert resolved
    Acknowledged --> Resolved: resolve
    Mitigated --> Resolved: resolve with rootCause
    Resolved --> [*]
```

Any other transition returns `409 Conflict` with problem details. The rules live in the domain's `Incident` aggregate; endpoints only bind input and map results.

| Transition | Who | Writes | Event | Timeline kind |
|---|---|---|---|---|
| create | operator, Alertmanager, Azure Monitor | incident, `ackDueAt`, `resolveDueAt`, level 1 | `incident.triggered` | `Triggered` |
| escalate | `CheckAcknowledgementSla` (API key) | level + 1, new `ackDueAt` | `incident.escalated` | `Escalated` |
| acknowledge | operator | `acknowledgedAt`, `assignee` | `incident.acknowledged` | `Acknowledged` |
| mitigate | operator, or alert resolved | `mitigatedAt` | `incident.mitigated` | `Mitigated` |
| resolve | operator only | `resolvedAt`, `rootCause` | `incident.resolved` | `Resolved` |
| note | operator | — | none | `Note` |
| alert repeat / resolve | alert source | — | none (or `incident.mitigated`) | `Alert` |

## SLA state

`slaState` is computed by the domain from the deadlines, the status and the current time whenever an incident is returned, so it never goes stale between writes.

```mermaid
flowchart TD
    A{Status} -- Resolved --> B{resolvedAt ≤ resolveDueAt?}
    B -- yes --> Met([Met])
    B -- no --> Breached([Breached])
    A -- Triggered --> C{now > ackDueAt<br/>or now > resolveDueAt?}
    A -- "Acknowledged / Mitigated" --> D{now > resolveDueAt?}
    C -- yes --> Breached
    D -- yes --> Breached
    C -- no --> E{remaining < 25%<br/>of active window?}
    D -- no --> E
    E -- yes --> AtRisk([AtRisk])
    E -- no --> OnTrack([OnTrack])
```

The active window is the acknowledgement window while `Triggered`, the resolve window afterwards. Windows per severity: [severity and SLA](../operations/severity-and-sla.md).

## Write path and read path

```mermaid
flowchart TB
    subgraph write ["Write path"]
        cmd["POST /api/incidents/{id}/..."] --> handler["Endpoint: bind, validate"]
        handler --> app["Application service"]
        app --> domain["Incident aggregate:<br/>transition + SLA rules"]
        app --> db[("Azure SQL")]
        app --> pub["Event publisher"]
        app --> hub["SignalR hub context"]
    end
    pub --> topic{{"incident-events"}}
    hub --> clients["Console clients"]
    subgraph read ["Read path"]
        q["GET /api/incidents"] --> db
        exp["GET /api/incidents/export"] --> db
        m["GET /api/metrics/summary"] --> db
    end
    exp --> insights["Insights"]
```

The API commits the state change first, then publishes the CloudEvent and the SignalR messages. If publishing fails after commit, the request still succeeds and the failure is logged with the incident id and alerted; the next state change republishes the full incident. A transactional outbox is the next step if publish failures appear in practice.

## End-to-end sequence

From creation to resolution, for a Sev2 acknowledged in time.

```mermaid
sequenceDiagram
    autonumber
    actor Op as Operator
    participant Web as Console
    participant API as Incidents API
    participant DB as Azure SQL
    participant Hub as SignalR
    participant Bus as incident-events
    participant Fn as Functions

    Op->>Web: New incident (Sev2, checkout)
    Web->>API: POST /api/incidents
    API->>DB: insert incident + Triggered entry
    API-->>Web: 201 Incident (ackDueAt = +30m)
    API->>Hub: IncidentChanged, TimelineAppended
    API->>Bus: incident.triggered
    Bus->>Fn: sla-scheduler → schedule check at ackDueAt
    Bus->>Fn: notifier → page primary
    Op->>Web: Acknowledge
    Web->>API: POST /acknowledge { actor }
    API->>DB: update status, acknowledgedAt
    API->>Hub: IncidentChanged
    API->>Bus: incident.acknowledged
    Note over Fn: check fires at ackDueAt,<br/>sees Acknowledged, completes as no-op
    Op->>Web: Mitigate (rollback done)
    Web->>API: POST /mitigate { actor, note }
    API->>Bus: incident.mitigated
    Op->>Web: Resolve with root cause
    Web->>API: POST /resolve { actor, rootCause }
    API->>DB: resolvedAt, rootCause, slaState Met
    API->>Hub: IncidentChanged
    API->>Bus: incident.resolved
```

## Event envelope

CloudEvents 1.0 structured mode, full incident in `data`, `eventType` and `severity` duplicated as Service Bus application properties for subscription filters. Rationale: [ADR 0006](../adr/0006-cloudevents-envelope.md).

```json
{
  "specversion": "1.0",
  "id": "5b1c0b0e-1f43-4c55-9a52-8f2d0e7a1c11",
  "type": "incident.escalated",
  "source": "incident-ops-api",
  "time": "2026-10-02T14:15:03Z",
  "subject": "8a6e2f4c-7d1b-4b8e-9a3f-2c5d6e7f8a9b",
  "datacontenttype": "application/json",
  "data": { "number": 1042, "severity": "Sev1", "status": "Triggered", "escalationLevel": 2, "ackDueAt": "2026-10-02T14:30:03Z" }
}
```
