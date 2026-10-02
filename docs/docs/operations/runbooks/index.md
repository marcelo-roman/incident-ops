# Runbooks

One runbook per alert or recurring failure. Each has: symptoms, impact, triage steps with KQL, mitigation, verification and escalation. KQL runs in the Application Insights resource (Logs blade) shared by every module; `cloud_RoleName` identifies the module.

| Runbook | Alerts that link to it | Default severity | Tier |
|---|---|---|---|
| [api-high-error-rate](api-high-error-rate.md) | Prometheus `ApiHighErrorRate`, `ApiHighLatencyP95`, `ApiDown`; Azure Monitor API failed requests, availability, response time | Sev2 (Sev1 for `ApiDown`) | L2 |
| [service-bus-dead-letters](service-bus-dead-letters.md) | Azure Monitor dead-lettered messages, Function failures | Sev2 | L2 |
| [sla-breach-escalation-not-firing](sla-breach-escalation-not-firing.md) | Prometheus `SlaBreachesOpen`; incident `Triggered` past `ackDueAt` + 5 min without `Escalated` entry | Sev2 | L2 |

How alerts reach these runbooks: [alerting](../alerting.md).

## Conventions used in the queries

| Item | Value |
|---|---|
| Role names | `incident-ops-api` (API), `func-incident-ops` (Functions), `incident-ops-insights` (Insights) |
| Incident id in logs | `customDimensions.incidentId` |
| Event type in logs | `customDimensions.eventType` |
| Function invocations | `requests` with `name` = function name |
| Correlation | `operation_Id` (W3C trace id), propagated over HTTP and Service Bus |

## Writing a new runbook

Copy the structure of an existing one. Every query must have been run against real data at least once before the runbook merges. Mark each step L1 or L2 so the [support model](../support-model.md) tiers know where they stop.
