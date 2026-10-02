# Architecture decision records

Format: [MADR](https://adr.github.io/madr/). One record per decision that is expensive to reverse. Records are immutable once accepted; a change of mind is a new record that supersedes the old one.

| # | Decision | Status |
| --- | --- | --- |
| [0001](0001-minimal-apis-over-controllers.md) | Minimal APIs over MVC controllers | Accepted |
| [0002](0002-service-bus-scheduled-messages-for-sla-timers.md) | Service Bus scheduled messages for SLA timers instead of polling | Accepted |
| [0003](0003-azure-signalr-service.md) | Azure SignalR Service for real-time updates | Accepted |
| [0004](0004-container-apps-for-hosting.md) | Azure Container Apps over App Service and AKS | Accepted |
| [0005](0005-bicep-for-infrastructure.md) | Bicep over Terraform | Accepted |
| [0006](0006-cloudevents-envelope.md) | CloudEvents 1.0 envelope for integration events | Accepted |
| [0007](0007-python-for-analytics-service.md) | Python for the analytics and RCA service | Accepted |
| [0008](0008-tactical-ddd-rich-aggregates-and-value-objects.md) | Tactical DDD with rich aggregates and value objects | Accepted |
| [0009](0009-transactional-outbox-for-integration-events.md) | Transactional outbox for integration events | Accepted |
| [0010](0010-monorepo-with-path-filtered-pipelines-and-codeowners.md) | Monorepo with path-filtered pipelines and CODEOWNERS | Accepted |

## When to write one

- It changes a contract in [contracts.md](https://github.com/marcelo-roman/incident-ops/blob/main/contracts/contracts.md).
- It picks a managed service, a framework or a hosting model.
- Reversing it would take more than one sprint.

## Review

An ADR is proposed as a pull request with status `Proposed`. It is discussed in the weekly architecture review (30 minutes, async comments first). It merges as `Accepted` with at least one reviewer from a team that consumes the decision.
