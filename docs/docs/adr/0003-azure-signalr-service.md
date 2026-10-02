---
status: accepted
date: 2026-09-09
deciders: Marcelo Roman
---

# Azure SignalR Service for real-time updates

## Context and problem statement

Operators on the console must see acknowledgements, escalations and notes from others within 2 seconds. The API runs on Container Apps with 0 to N replicas. How do we push updates to browsers across replicas?

## Decision drivers

- Fan-out must work regardless of which replica handled the write.
- API replicas can scale to zero without dropping client connections.
- Same programming model locally (no Azure dependency) and in production.

## Considered options

1. Azure SignalR Service (default mode), `Free_F1`
2. Self-hosted SignalR with a Redis backplane
3. Client polling every 5 seconds
4. Azure Web PubSub

## Decision outcome

Chosen option: **Azure SignalR Service**. The hub at `/hubs/incidents` is the same code locally (in-process) and in production (`AddAzureSignalR()` when the connection setting is present).

### Consequences

- Good: client connections terminate at the service, so API replicas can scale to zero and restart freely.
- Good: no backplane to operate.
- Good: `Free_F1` covers 20 concurrent connections and 20 000 messages a day, enough for the demo; `Standard_S1` (1 000 connections per unit) is a SKU change, not a code change.
- Bad: Free tier has no SLA. Accepted for a demo; production would run `Standard_S1`.
- Bad: one more resource to provision and monitor.

## Pros and cons of the options

### Redis backplane

- Good: full control.
- Bad: replicas hold the connections, so scale-to-zero drops all clients; Redis is an extra stateful service.

### Polling

- Good: trivial.
- Bad: 5 s worst case misses the 2 s target; N operators × 12 requests a minute of pure overhead.

### Web PubSub

- Good: protocol-agnostic, larger scale.
- Bad: loses SignalR hub semantics and the `@microsoft/signalr` client the team already uses.
