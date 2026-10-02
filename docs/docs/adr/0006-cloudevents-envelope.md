---
status: accepted
date: 2026-09-10
deciders: Marcelo Roman
---

# CloudEvents 1.0 envelope for integration events

## Context and problem statement

The API publishes incident lifecycle events to Service Bus, consumed by Functions today and potentially by other teams later (reporting, a status page). What shape do messages take?

## Decision drivers

- Consumers in .NET and Python must parse events without sharing a library.
- Metadata (type, source, id, time, subject) must be uniform for routing, deduplication and tracing.
- Subscription filters must not have to parse the body.

## Considered options

1. CloudEvents 1.0 structured JSON, with `eventType` and `severity` duplicated as application properties
2. Custom envelope (`{ eventName, payload }`)
3. Bare payload with metadata only in application properties

## Decision outcome

Chosen option: **CloudEvents 1.0 structured mode** (`application/cloudevents+json`). `data` carries the full `Incident`. The Service Bus message also sets `eventType` and `severity` application properties so SQL filters on subscriptions stay cheap. `id` is the Service Bus `MessageId`, enabling duplicate detection.

### Consequences

- Good: a vendor-neutral, documented contract; SDKs exist for .NET and Python; Event Grid speaks it natively if we move there.
- Good: `subject` = incident id makes per-incident tracing and replay simple.
- Good: full incident in `data` means consumers do not call back for state on most paths (event-carried state transfer).
- Bad: type and severity exist twice (envelope/data and properties). The publisher sets both from the same value in one place.
- Bad: full state in every event means larger messages (~1–2 KB), negligible at this volume.

## Pros and cons of the options

### Custom envelope

- Good: shortest path to code.
- Bad: every new consumer learns a bespoke format; no tooling.

### Properties-only metadata

- Good: smallest bodies.
- Bad: metadata lost when a message is forwarded or exported; tied to Service Bus.
