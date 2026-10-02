# SLA timers and escalation

An unacknowledged incident climbs one escalation level per missed acknowledgement window, up to level 3. No component polls: the timer is a Service Bus scheduled message. Rationale: [ADR 0002](../adr/0002-service-bus-scheduled-messages-for-sla-timers.md).

## Deadlines

- On creation: `ackDueAt = createdAt + ack window`, `resolveDueAt = createdAt + resolve window`.
- On each escalation: `ackDueAt = now + ack window`; `resolveDueAt` does not move.
- Windows: Sev1 15 min / 4 h, Sev2 30 min / 8 h, Sev3 4 h / 3 days, Sev4 24 h / 10 days ([severity and SLA](../operations/severity-and-sla.md)).

## Timer loop

```mermaid
sequenceDiagram
    autonumber
    participant API as Incidents API
    participant Topic as incident-events
    participant Sched as ScheduleSlaCheck
    participant Queue as sla-checks
    participant Check as CheckAcknowledgementSla
    participant Notify as NotifyOnCall
    participant Logic as Logic App

    API->>Topic: incident.triggered (Sev1, level 1, ackDueAt +15m)
    par timer
        Topic->>Sched: sla-scheduler subscription
        Sched->>Queue: ScheduleMessage({ incidentId, level 1 }, ackDueAt)
    and page
        Topic->>Notify: notifier subscription (Sev1/Sev2)
        Notify->>Logic: { INC-1042, level 1, target primary }
    end
    Note over Queue: message invisible until ackDueAt
    Queue->>Check: delivered at ackDueAt
    Check->>API: GET /api/incidents/{id}
    alt status Triggered and level == 1
        Check->>API: POST /api/incidents/{id}/escalate (X-Api-Key)
        API->>API: level 2, ackDueAt = now + 15m
        API->>Topic: incident.escalated
        Topic->>Sched: schedule { level 2 } at new ackDueAt
        Topic->>Notify: page secondary
    else acknowledged, resolved or level changed
        Check->>Check: complete message (no-op)
    end
    Note over API,Logic: one more cycle reaches level 3 (engineering lead), where escalation stops
```

## Idempotency guard

Messages can be delivered twice, late, or race with a human acknowledgement. The check compares the message's level with the incident's current level and does nothing on mismatch.

```mermaid
flowchart TD
    M["sla-checks message<br/>{ incidentId, escalationLevel: n }"] --> G["GET incident"]
    G --> NF{404?}
    NF -- yes --> C1["complete, log warning"]
    NF -- no --> S{status == Triggered?}
    S -- no --> C2["complete: acknowledged in time"]
    S -- yes --> L{incident.level == n?}
    L -- no --> C3["complete: stale or duplicate"]
    L -- yes --> T{n < 3?}
    T -- no --> C4["complete: already at engineering lead"]
    T -- yes --> E["POST /escalate"]
    E --> R{success?}
    R -- yes --> C5["complete"]
    R -- "no (5xx, timeout)" --> A["abandon → retry,<br/>dead-letter after 10"]
```

No message is ever cancelled. Acknowledging an incident simply turns its pending check into a no-op, which removes a whole class of race conditions.

## On-call rotation

Six engineers, weekly shifts starting Monday 10:30 America/New_York. Each engineer is secondary the week before being primary, so they take the pager with context on what is in flight.

```mermaid
gantt
    title Rotation (example, six engineers A–F)
    dateFormat YYYY-MM-DD
    axisFormat %b %d
    section Primary
    A :p1, 2026-09-28, 7d
    B :p2, after p1, 7d
    C :p3, after p2, 7d
    D :p4, after p3, 7d
    E :p5, after p4, 7d
    F :p6, after p5, 7d
    section Secondary
    B :s1, 2026-09-28, 7d
    C :s2, after s1, 7d
    D :s3, after s2, 7d
    E :s4, after s3, 7d
    F :s5, after s4, 7d
    A :s6, after s5, 7d
```

`GET /api/oncall/current` returns `{ weekStart, primary, secondary, lead }` for the current week.

## Escalation levels

```mermaid
flowchart LR
    T(["Incident Triggered"]) --> L1["Level 1<br/>primary on call"]
    L1 -- "ack window missed" --> L2["Level 2<br/>secondary on call"]
    L2 -- "ack window missed" --> L3["Level 3<br/>engineering lead"]
    L3 -- "ack window missed" --> X["no further automatic escalation;<br/>IC escalates by hand"]
    L1 -- acknowledged --> A(["Acknowledged"])
    L2 -- acknowledged --> A
    L3 -- acknowledged --> A
```

| Severity | Level 1 → 2 after | Level 2 → 3 after | Lead paged at the latest |
| --- | --- | --- | --- |
| Sev1 | 15 min | 15 min | 30 min after trigger |
| Sev2 | 30 min | 30 min | 1 h |
| Sev3 | 4 h | 4 h | 8 h (not paged; queue) |
| Sev4 | 24 h | 24 h | 48 h (not paged; queue) |

Paging only happens for Sev1 and Sev2 (`notifier` subscription filter). Sev3 and Sev4 escalate in the record, which moves the assignee's queue, without waking anyone. Human escalation paths beyond level 3: [incident response](../operations/incident-response.md#escalation-paths).
