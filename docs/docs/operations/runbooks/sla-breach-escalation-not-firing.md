# Runbook: SLA breach escalation not firing

| | |
| --- | --- |
| Alert | an incident is `Triggered` more than 5 minutes past `ackDueAt` with no `Escalated` timeline entry for that window |
| Default severity | Sev2 (on-call paging is the safety net for every other incident) |
| Tier | L2 |

## How escalation works

`incident.triggered` / `incident.escalated` → `sla-scheduler` subscription → `ScheduleSlaCheck` schedules a message on `sla-checks` at `ackDueAt` → `CheckAcknowledgementSla` fires → if still `Triggered` at the same level, `POST /api/incidents/{id}/escalate` → API emits `incident.escalated` with a new `ackDueAt`. Stops at level 3. Diagrams: [SLA timers and escalation](../../architecture/sla-escalation.md).

Each arrow can break. Triage walks the chain in order.

## Triage

**1. Which incidents are overdue (L1/L2).**

```bash
curl -s -H "X-Api-Key: $ESCALATION_API_KEY" "https://incidents-api.marceloroman.com.br/api/incidents?status=Triggered&open=true" \
  | jq --arg now "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
    '[.[] | select(.ackDueAt < $now and .escalationLevel < 3) | {number, severity, escalationLevel, ackDueAt}]'
```

Take one `id` and follow it through the steps below; set it once:

```kusto
let incidentId = "<incident id>";
```

**2. Did the API publish the event?**

```kusto
let incidentId = "<incident id>";
let operations = traces
    | where timestamp > ago(1d)
    | where cloud_RoleName == "incident-ops-api"
    | where tostring(customDimensions.incidentId) == incidentId
    | distinct operation_Id;
union requests, dependencies, exceptions
| where timestamp > ago(1d)
| where cloud_RoleName == "incident-ops-api"
| where operation_Id in (operations)
| project timestamp, itemType, name, type, target, success, resultCode, outerMessage
| order by timestamp asc
```

No Service Bus dependency call after the create/escalate request → publish failed or was skipped: check `exceptions` for the API in that window, then [api-high-error-rate](api-high-error-rate.md#mitigation) option B.

**3. Did `ScheduleSlaCheck` run and schedule?**

```kusto
let incidentId = "<incident id>";
union requests, traces
| where timestamp > ago(1d)
| where cloud_RoleName == "func-incident-ops"
| where operation_Name in ("ScheduleSlaCheck", "CheckAcknowledgementSla")
| where tostring(customDimensions.incidentId) == incidentId
| project timestamp, operation_Name, itemType, success, message, customDimensions.escalationLevel
| order by timestamp asc
```

No `ScheduleSlaCheck` invocation → message stuck or dead-lettered on `sla-scheduler`, or Function App not running:

```bash
az servicebus topic subscription show -g rg-incident-ops --namespace-name <namespace> \
  --topic-name incident-events -n sla-scheduler --query "{active:countDetails.activeMessageCount, dead:countDetails.deadLetterMessageCount}"
az functionapp show -g rg-incident-ops -n func-incident-ops --query state
```

Also verify the subscription rule still matches: the filter must read `eventType IN ('incident.triggered','incident.escalated')`.

```bash
az servicebus topic subscription rule list -g rg-incident-ops --namespace-name <namespace> \
  --topic-name incident-events --subscription-name sla-scheduler --query "[].filter.sqlExpression"
```

**4. Is the check scheduled for the right time?** Scheduled messages count as `scheduledMessageCount` on `sla-checks`:

```bash
az servicebus queue show -g rg-incident-ops --namespace-name <namespace> -n sla-checks \
  --query "countDetails.scheduledMessageCount"
```

Compare the scheduled time in the `ScheduleSlaCheck` log entry with the incident's `ackDueAt`. A shift of exactly N hours means a time zone bug (local time instead of UTC).

**5. Did `CheckAcknowledgementSla` run, and what did it decide?**

```kusto
let incidentId = "<incident id>";
requests
| where timestamp > ago(1d)
| where cloud_RoleName == "func-incident-ops" and name == "CheckAcknowledgementSla"
| join kind=inner (
    traces
    | where tostring(customDimensions.incidentId) == incidentId
    | project operation_Id, message, decision = tostring(customDimensions.decision)
  ) on operation_Id
| project timestamp, success, resultCode, decision, message
| order by timestamp asc
```

Ran but decided no-op while the incident was `Triggered` at the same level → defect in the guard; ran and failed → [service-bus-dead-letters](service-bus-dead-letters.md#triage) step 3.

**6. Fleet-wide view: escalations per hour vs overdue incidents.** A sudden drop to zero while incidents are open points at the pipeline, not at a single incident:

```kusto
requests
| where timestamp > ago(7d)
| where cloud_RoleName == "incident-ops-api"
| where name has "escalate"
| summarize escalations = count(), failed = countif(success == false) by bin(timestamp, 1h)
| render timechart
```

## Mitigation

1. **Manual escalation now (L2).** For each overdue incident, page the next level directly (contacts from `GET /api/oncall/current`) and record it as a note. If the on-call has access to the API key (the `ESCALATION_API_KEY` deployment secret), also call `POST /api/incidents/{id}/escalate` with `X-Api-Key`: it raises the level, emits `incident.escalated` and restarts the loop.
2. **Restore the chain** at the broken step: republish (resubmit dead letters), fix the subscription rule via Bicep redeploy, restart or roll back the Function App, fix the API key.
3. **Backfill timers.** For overdue incidents whose chain broke before scheduling, resubmitting their `sla-scheduler` dead letters or escalating once manually restarts the loop.

## Verification

- Query 1 returns an empty list (or only incidents at level 3).
- Query 6 shows escalations resuming.
- For a test incident (Sev4 created in production with title prefix `[TEST]`), `ScheduleSlaCheck` logs a scheduled message with the right `ackDueAt`. Resolve it afterwards.

## Escalation

- Chain not restored within 1 hour: engineering lead; the on-call keeps manual escalation until fixed.
- Repeated occurrence: postmortem even at Sev2, since this is the safety net for every other incident.
