---
status: accepted
date: 2026-10-02
deciders: Marcelo Roman
---

# Transactional outbox for integration events

## Context and problem statement

Every incident change must be persisted in Azure SQL, pushed to the console over SignalR and published as a CloudEvent on Service Bus, where it starts the escalation timers and pages the on-call engineer. If the database commit succeeds and the publish fails, an incident exists that no timer watches: escalation never fires. If the publish happens first and the commit fails, consumers act on an incident that does not exist. How does the API guarantee that a committed change is eventually published?

## Decision drivers

- No committed state change without its event; no event without a committed change.
- The API keeps accepting writes while Service Bus is unavailable.
- Consumers can deduplicate.
- No distributed transaction (Service Bus does not enlist in SQL transactions).

## Considered options

1. Transactional outbox: domain events become outbox rows in the same transaction, a dispatcher publishes them
2. Publish after commit, in the request
3. Change data capture on the incidents table

## Decision outcome

Chosen option: **transactional outbox**. The unit of work translates the domain events raised by the aggregate into outbox rows and saves them in the same `SaveChanges` as the aggregate. A hosted `OutboxDispatcher`, signaled on commit and polling as a fallback, notifies SignalR, publishes the CloudEvent and marks the row processed. Failures are retried with capped exponential backoff and dead-lettered after `Outbox__MaxAttempts`. The CloudEvent `id` and the Service Bus `MessageId` equal the outbox message id.

### Consequences

- Good: a Service Bus outage delays timers and pages instead of losing them; the API keeps serving.
- Good: delivery is at least once with a stable id, so consumers deduplicate and Service Bus duplicate detection can be enabled.
- Good: real-time notifications and integration events leave through the same path, with the same retry policy and the same tests.
- Bad: order is preserved per dispatch batch but not across retries; consumers act on the state carried in the event (the SLA check only escalates while the incident is still `Triggered` at the same level).
- Bad: several API replicas would each run a dispatcher; claiming rows with a lease is needed before scaling out.
- Bad: one more table and a background service to operate; dead-lettered outbox rows need the same attention as Service Bus dead letters.

## Pros and cons of the options

### Publish after commit

- Good: no extra table.
- Bad: a crash or a Service Bus error between commit and publish loses the event silently.

### Change data capture

- Good: no application code for publishing.
- Bad: needs a capture pipeline and a connector to Service Bus; events would describe rows, not domain facts.
