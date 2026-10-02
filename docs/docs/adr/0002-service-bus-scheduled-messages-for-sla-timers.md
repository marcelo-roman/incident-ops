---
status: accepted
date: 2026-09-08
deciders: Marcelo Roman
---

# Service Bus scheduled messages for SLA timers instead of polling

## Context and problem statement

An incident not acknowledged by `ackDueAt` must escalate one level, up to level 3. A Sev1 has a 15-minute acknowledgement window, so the timer must be accurate to well under a minute, and it must fire even when the API receives no traffic. How do we implement per-incident timers?

## Decision drivers

- Accuracy: escalation within 60 s of `ackDueAt`.
- No double escalation under retries, duplicates or concurrent acknowledgement.
- Zero cost and zero work when no incident is open.
- Survives restarts and scale-to-zero of every compute component.

## Considered options

1. Service Bus scheduled messages on a `sla-checks` queue, one per incident per level
2. Timer-triggered function polling `GET /api/incidents?open=true` every minute
3. Durable Functions durable timers, one orchestration per incident
4. Background `IHostedService` in the API

## Decision outcome

Chosen option: **scheduled messages**. `ScheduleSlaCheck` schedules `{ incidentId, escalationLevel }` at `ackDueAt` on every `incident.triggered` and `incident.escalated`. `CheckAcknowledgementSla` escalates only if the incident is still `Triggered` at the same level.

### Consequences

- Good: the timer is durable in the broker; no compute runs while waiting.
- Good: the level guard makes processing idempotent; acknowledgement simply makes the pending message a no-op, so nothing has to be cancelled.
- Good: dead-lettering gives a visible, replayable failure path.
- Bad: timer state is not queryable as a list ("what escalates next?"). Mitigated by `ackDueAt` on the incident, which the console sorts by.
- Bad: depends on Service Bus being available at trigger time; covered by the [escalation runbook](../operations/runbooks/sla-breach-escalation-not-firing.md) and an alert on the absence of `Escalated` timeline entries for breached incidents.

## Pros and cons of the options

### Polling timer function

- Good: simplest code; easy to reason about.
- Bad: up to one interval of extra latency; 1 440 executions a day when nothing is open; every poll loads the API and database (and wakes serverless SQL from pause).
- Bad: concurrency between overlapping polls needs a lock.

### Durable Functions timers

- Good: timers and cancellation are first-class.
- Bad: adds a storage-backed orchestration state store and replay semantics for a single-step workflow; harder to operate and to explain in a runbook.

### Hosted service in the API

- Good: no extra infrastructure.
- Bad: Container Apps scales the API to zero; timers die with the replica. Multiple replicas need leader election.
