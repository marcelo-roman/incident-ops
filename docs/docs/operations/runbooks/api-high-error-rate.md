# Runbook: API high error rate

| | |
| --- | --- |
| Alert | Prometheus `ApiHighErrorRate` (5xx > 5% for 2 min), `ApiDown`, `ApiHighLatencyP95`; Azure Monitor failed requests > 5% for 5 min |
| Default severity | Sev2; Sev1 if > 25% of requests or all writes failing |
| Tier | L2 |
| Related SLO | [API availability 99.5%](../slo.md) |

## Symptoms

- Console shows errors loading or updating incidents.
- Availability test on `/health/ready` failing.
- Functions logging failures calling `POST /escalate` (escalations stall; see [sla-breach-escalation-not-firing](sla-breach-escalation-not-firing.md)).

## Triage

**1. Confirm and size it (L1/L2).** Error rate per 5 minutes over the last 3 hours:

```kusto
requests
| where timestamp > ago(3h)
| where cloud_RoleName == "incident-ops-api"
| summarize total = count(), failed = countif(toint(resultCode) >= 500) by bin(timestamp, 5m)
| extend errorRate = round(100.0 * failed / total, 2)
| order by timestamp asc
| render timechart
```

Same question in Prometheus (local stack or self-hosted):

```promql
sum by (http_route) (rate(http_server_request_duration_seconds_count{job="incident-ops-api", http_response_status_code=~"5.."}[5m]))
/ ignoring(http_route) group_left
sum(rate(http_server_request_duration_seconds_count{job="incident-ops-api"}[5m]))
```

If `ApiDown` fired, check the container first: `docker compose ps api` locally, `az containerapp revision list` in Azure (step 5), and skip to Mitigation D if the latest revision is unhealthy.

**2. Which endpoints (L2).**

```kusto
requests
| where timestamp > ago(1h)
| where cloud_RoleName == "incident-ops-api"
| summarize total = count(), failed = countif(toint(resultCode) >= 500), p95 = percentile(duration, 95) by name
| extend errorRate = round(100.0 * failed / total, 2)
| where failed > 0
| order by failed desc
```

All endpoints failing → infrastructure or a shared dependency (SQL, startup). Only writes failing → Service Bus or SignalR publish path, or the database in read-only.

**3. Top exceptions (L2).**

```kusto
exceptions
| where timestamp > ago(1h)
| where cloud_RoleName == "incident-ops-api"
| summarize count = count(), sample = any(outerMessage) by type, method = tostring(details[0].parsedStack[0].method)
| order by count desc
| take 10
```

**4. Failing dependencies (L2).**

```kusto
dependencies
| where timestamp > ago(1h)
| where cloud_RoleName == "incident-ops-api"
| summarize total = count(), failed = countif(success == false), p95 = percentile(duration, 95) by type, target
| order by failed desc
```

| Dependency failing | Likely cause | Go to |
| --- | --- | --- |
| `SQL` with timeouts | DTU cap of the Basic database, blocking query | Mitigation A |
| `Azure Service Bus` | namespace throttling or outage; RBAC change | Mitigation B |
| `Azure SignalR` | Free tier quota (20 000 messages/day) exhausted | Mitigation C |
| none, exceptions in app code | bad deploy or configuration change | Mitigation D |

**5. What changed (L2).** Correlate the error start with deploys and configuration changes:

```kusto
requests
| where timestamp > ago(24h)
| where cloud_RoleName == "incident-ops-api"
| summarize requests = count(), failed = countif(toint(resultCode) >= 500) by application_Version, cloud_RoleInstance, bin(timestamp, 15m)
| order by timestamp asc
```

A new `application_Version` at the start of the errors points at the deploy. Also check: the `production` environment deployment history in GitHub, Container App revision list, activity log for configuration changes (scale rules, secrets, environment variables), and [Azure Service Health](https://status.azure.com).

## Mitigation

**A. SQL.** Check whether DTU or workers are at the cap:

```bash
az sql db show -g rg-incident-ops -s <server> -n <db> --query "{sku:currentSku, status:status}"
az monitor metrics list --resource <db-resource-id> --metric dtu_consumption_percent workers_percent --interval PT5M
```

Scale the database to Standard S0 (10 DTU) temporarily with `az sql db update --service-objective S0`; open a work item to find the query. The next power down and up re-creates it as Basic.

**B. Service Bus.** The API commits the incident and its outbox rows in one transaction, so reads and writes keep working; the outbox dispatcher retries with capped backoff and delivers the events once Service Bus recovers, so escalation timers start late rather than never. Check namespace health and the API's managed identity role assignment (`Azure Service Bus Data Sender`). Pending escalations must be watched manually until fixed ([sla-breach-escalation-not-firing](sla-breach-escalation-not-firing.md)).

**C. SignalR.** Live updates stop; the API should not return 5xx for this. If it does, it is a bug: restart is not a fix. Raise to Standard_S1 if quota is the cause.

**D. Bad deploy or configuration.** Roll back first, investigate second:

```bash
az containerapp revision list -n ca-incident-ops-api -g rg-incident-ops -o table
az containerapp revision copy -n ca-incident-ops-api -g rg-incident-ops --from-revision <previous-revision>
```

Then revert the PR on `main` so the next deploy does not reintroduce it.

## Verification

- Query 1 shows error rate below 0.5% for 15 minutes.
- Availability test green.
- Console loads open incidents and an acknowledge round-trips.
- Escalations resumed: run query 1 of [sla-breach-escalation-not-firing](sla-breach-escalation-not-firing.md#triage).

## Escalation

- Not mitigated in 30 min (Sev2) or 15 min (Sev1): engineering lead.
- Azure platform suspected: Azure support request, record case number in the incident notes.
- Data integrity concern (writes partially applied): Sev1, engineering lead immediately.
