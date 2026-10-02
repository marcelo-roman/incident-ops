---
status: accepted
date: 2026-10-02
deciders: Marcelo Roman
---

# Tactical DDD with rich aggregates and value objects

## Context and problem statement

The rules that matter most in Incident Ops are small and easy to get subtly wrong: which status transitions are allowed, when an acknowledgement window opens and closes, when escalation stops, when an incident complies with its SLA, when a check that fires late must do nothing. The same rules are read by three services. Where do these rules live, and how is it guaranteed that they are not reimplemented in endpoints, handlers or triggers?

## Decision drivers

- One implementation per rule inside each bounded context, testable without a database, a host or a network.
- Invalid states (level 4, an empty title, a naive timestamp, a negative window) cannot be represented.
- Endpoints, triggers and routers stay thin enough to review at a glance.
- The structure is checkable by a build step, not only by review.

## Considered options

1. Rich aggregates and value objects per bounded context, with domain events and use cases that only orchestrate
2. Anemic entities with service classes holding the rules (transaction script)
3. One shared domain library used by every service

## Decision outcome

Chosen option: **rich aggregates and value objects per bounded context**.

- Incident Management (`services/api`): `Incident` is the aggregate root; it owns its `TimelineEntry` entities and an `SlaClock` value object, enforces transitions and raises one domain event per behavior. `Service` and `OnCallRotation` are separate aggregates. Repositories exist only for roots.
- Escalation (`services/functions`): `AcknowledgementWatch` is the aggregate; `EscalationLevel`, `AcknowledgementDeadline` and `Severity` carry the rules; decisions are returned as `EscalationDecision` values.
- Operational Analytics (`services/insights`): `IncidentRecord` and `IncidentHistory` answer measurement questions; value objects (`ReportingWindow`, `Percentage`, `SlaPolicy`) reject invalid input; domain services hold computation across many records.
- Use cases load, call one behavior and commit or act through ports. Architecture tests (NetArchTest, reflection) and import-linter contracts fail the build when a layer reaches the wrong way or a domain type gains a public setter.

### Consequences

- Good: the rules are unit tested in milliseconds (109 domain tests in the API, decision tables for `AcknowledgementWatch.Evaluate` and `PagingDecision.Decide`).
- Good: handlers, triggers and routers have no branches about the domain, so review focuses on wiring and tests.
- Good: each context keeps its own language; the contract is the only shared artifact ([context map](../architecture/context-map.md)).
- Bad: more types and mapping code: EF Core value converters and an owned type for `SlaClock`, anti-corruption translators in Escalation and Analytics.
- Bad: the SLA compliance rule exists in Incident Management and in Analytics. Both follow the contract definition and are pinned by tests in each module.

## Pros and cons of the options

### Anemic entities with services

- Good: fewer types; maps directly to tables.
- Bad: nothing stops a second service from changing state without the rule; invariants depend on discipline.

### Shared domain library

- Good: one copy of the SLA rule.
- Bad: couples release cycles of .NET and Python consumers (Python cannot use it at all); every context inherits the vocabulary of the system of record.
