# Runbook: Service Bus dead letters

| | |
|---|---|
| Alert | `DeadletteredMessages` > 0 on `sla-checks`, `incident-events/sla-scheduler` or `incident-events/notifier` |
| Default severity | Sev2 for `sla-checks` and `sla-scheduler` (escalations at risk); Sev3 for `notifier` |
| Tier | L2 |


## Why it matters

| Entity | A dead letter means |
|---|---|
| `incident-events/sla-scheduler` | an incident has no SLA timer; it will not escalate |
| `sla-checks` | a timer fired but the check failed; the incident did not escalate |
| `incident-events/notifier` | someone was not paged |

Messages dead-letter after 10 failed deliveries (`MaxDeliveryCount`) or when expired. They do not retry on their own.

## Triage

**1. Count per entity (L1/L2).** Requires diagnostic settings exporting namespace metrics to Log Analytics:

```kusto
AzureMetrics
| where TimeGenerated > ago(24h)
| where ResourceProvider == "MICROSOFT.SERVICEBUS"
| where MetricName == "DeadletteredMessages"
| summarize deadLetters = max(Maximum) by EntityName = tostring(split(_ResourceId, "/")[-1]), bin(TimeGenerated, 15m)
| where deadLetters > 0
| order by TimeGenerated desc
```

Or directly:

```bash
az servicebus queue show -g rg-incident-ops --namespace-name <namespace> -n sla-checks --query countDetails
az servicebus topic subscription show -g rg-incident-ops --namespace-name <namespace> --topic-name incident-events -n sla-scheduler --query countDetails
```

**2. Why the function failed (L2).**

```kusto
requests
| where timestamp > ago(24h)
| where cloud_RoleName == "func-incident-ops"
| where success == false
| summarize failures = count(), lastSeen = max(timestamp) by name, resultCode
| order by failures desc
```

```kusto
exceptions
| where timestamp > ago(24h)
| where cloud_RoleName == "func-incident-ops"
| summarize count = count(), sample = any(outerMessage), incidents = make_set(tostring(customDimensions.incidentId), 20) by operation_Name, type
| order by count desc
```

**3. Failed calls from Functions to the API (L2).** Most check failures are the API refusing or failing the escalation:

```kusto
dependencies
| where timestamp > ago(24h)
| where cloud_RoleName == "func-incident-ops"
| where type == "HTTP" and name has "escalate"
| summarize calls = count(), failed = countif(success == false) by resultCode, bin(timestamp, 15m)
| order by timestamp desc
```

| Result code | Meaning | Action |
|---|---|---|
| `401` | `X-Api-Key` mismatch (key rotated on one side only) | Mitigation A |
| `409` | transition rejected; incident moved on | Mitigation B |
| `404` | incident does not exist (seed reset, wrong environment) | dead letter can be discarded after confirming |
| `5xx` / timeout | API unhealthy | [api-high-error-rate](api-high-error-rate.md), then replay |
| no dependency call | deserialization or code failure before calling | Mitigation C |

**4. Inspect the messages (L2).** Portal → Service Bus namespace → entity → Service Bus Explorer → Dead-letter → Peek. Read `DeadLetterReason`, `DeadLetterErrorDescription` and the body (`{ incidentId, escalationLevel }` for `sla-checks`, a CloudEvent for subscriptions).

## Mitigation

**A. API key mismatch.** The Function App reads the key from its `IncidentsApi__ApiKey` app setting; the API compares against its `escalation-api-key` Container Apps secret. Both come from the `ESCALATION_API_KEY` deployment secret; redeploy the infrastructure (or align both settings) and restart the Function App so it re-reads the setting:

```bash
az functionapp restart -g rg-incident-ops -n func-incident-ops
```

**B. 409 Conflict.** The check should treat a non-`Triggered` incident or a level mismatch as a no-op and complete the message. A 409 that dead-letters is a defect in the level guard: open a Bug, discard the dead letters after confirming each incident's state in the console.

**C. Code failure.** Roll back the Function App to the previous successful deployment by re-running the deploy workflow for the previous commit on `main`.

**Open incidents at risk.** While escalation is broken, the on-call primary watches `Triggered` incidents manually (query 1 in [sla-breach-escalation-not-firing](sla-breach-escalation-not-firing.md#triage)) and escalates by hand in the console.

## Replay

Only after the cause is fixed.

- `sla-checks`: resubmit from Service Bus Explorer (Dead-letter → select → Re-send). The level guard makes stale checks no-ops, so replaying is safe.
- `sla-scheduler`: resubmitting reschedules a check at the original `ackDueAt`; if that is in the past, it fires immediately and escalates if still unacknowledged. Intended.
- `notifier`: replay only for incidents still open; a page for a resolved incident is noise.

Record count replayed and discarded in the incident notes.

## Verification

- Dead-letter count back to 0 and not increasing for 30 minutes.
- Query 2 shows successful invocations after the fix.
- Every open `Triggered` incident past `ackDueAt` has an `Escalated` timeline entry.

## Escalation

- Cause not identified within 30 min: engineering lead.
- Service Bus platform issue: Azure support request.
