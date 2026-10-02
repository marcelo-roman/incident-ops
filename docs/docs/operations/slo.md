# Service level objectives

SLIs and SLOs for the Incidents API (`services/api`, role `incident-ops-api`), measured from Application Insights, and the policy for spending the error budget.

## SLIs and SLOs

Window: rolling 28 days. Excludes `/health/*` and requests rejected by rate limiting (`429`).

| SLI | Definition | SLO |
| --- | --- | --- |
| Availability | requests with `resultCode < 500` ÷ all requests | 99.5% |
| Read latency | `GET /api/*` with duration ≤ 300 ms ÷ all `GET /api/*` | 95% |
| Write latency | `POST /api/*` with duration ≤ 800 ms ÷ all `POST /api/*` | 95% |
| Real-time freshness | `IncidentChanged` sent within 2 s of the commit | 99% |
| Escalation timeliness | escalations performed within 60 s of `ackDueAt` | 99% |

Why these numbers: the API runs on consumption infrastructure with one warm replica and a Basic database capped at 5 DTU, and the demo environment is powered on only for evaluations, so the SLOs are measured while it is on. 99.5% leaves room for scale-out cold starts and DTU saturation under bursts while still flagging real regressions. Escalation timeliness is held tighter because it is the safety net for every other service.

## Error budget

| SLO | Budget per 28 days |
| --- | --- |
| Availability 99.5% | 0.5% of requests; at 50 000 requests/28 d, 250 failed requests (≈ 3 h 22 min of full outage) |
| Escalation timeliness 99% | 1 late escalation in 100 |

## Queries

Availability SLI over the window:

```kusto
requests
| where timestamp > ago(28d)
| where cloud_RoleName == "incident-ops-api"
| where name !startswith "GET /health" and resultCode != "429"
| summarize total = count(), good = countif(toint(resultCode) < 500)
| extend sli = round(100.0 * good / total, 3), budgetUsedPct = round(100.0 * (total - good) / (total * 0.005), 1)
```

Latency SLIs:

```kusto
requests
| where timestamp > ago(28d)
| where cloud_RoleName == "incident-ops-api"
| where name startswith "GET /api" or name startswith "POST /api"
| extend method = tostring(split(name, " ")[0])
| extend threshold = iff(method == "GET", 300.0, 800.0)
| summarize total = count(), good = countif(duration <= threshold) by method
| extend sli = round(100.0 * good / total, 2)
```

Escalation timeliness (Functions log the delay between `ackDueAt` and execution as `customDimensions.delaySeconds`):

```kusto
traces
| where timestamp > ago(28d)
| where cloud_RoleName == "func-incident-ops"
| where operation_Name == "CheckAcknowledgementSla" and tostring(customDimensions.decision) == "Escalated"
| summarize total = count(), onTime = countif(todouble(customDimensions.delaySeconds) <= 60)
| extend sli = round(100.0 * onTime / total, 2)
```

## Alerting on burn rate

Multi-window burn-rate alerts on availability (burn rate 1 = spending the budget exactly over 28 days):

| Alert | Long window | Short window | Burn rate | Budget spent at trigger | Severity |
| --- | --- | --- | --- | --- | --- |
| Fast burn | 1 h | 5 min | 14.4 | 2% | Sev2, page |
| Slow burn | 6 h | 30 min | 6 | 5% | Sev3, ticket |

```kusto
let slo = 0.995;
let burn = (window: timespan) {
    requests
    | where timestamp > ago(window)
    | where cloud_RoleName == "incident-ops-api"
    | where name !startswith "GET /health" and resultCode != "429"
    | summarize errorRatio = 1.0 * countif(toint(resultCode) >= 500) / count()
    | extend burnRate = errorRatio / (1 - slo)
};
union (burn(1h) | extend window = "1h"), (burn(5m) | extend window = "5m")
```

Fast burn fires when both windows exceed 14.4. Low traffic can make one failed request look like a high burn; the alert rule requires at least 20 requests in the short window.

## Error budget policy

| Budget remaining (28 d) | Policy |
| --- | --- |
| > 50% | Normal. Releases follow the [release readiness checklist](../engineering/release-readiness-checklist.md). |
| 25–50% | Releases that touch the failing path need a second reviewer and a canary at 10% for 30 minutes. |
| 0–25% | Feature releases to the API pause unless the Product Owner and engineering lead accept the risk in writing. Reliability work moves to the top of the sprint. |
| Exhausted | Freeze on feature releases until the budget recovers above 0 on the rolling window. Only reliability fixes and security patches ship. A postmortem is required if a single incident consumed > 20% of the budget. |

The policy is agreed with Product in advance so it is a rule applied, not a negotiation during an incident. Disagreements escalate to the head of engineering and the product lead, with the decision recorded in the weekly [stakeholder update](../leadership/stakeholder-updates.md).

## Review

Monthly, in the ops review: SLI per objective, budget consumed and by which incidents, whether objectives are still the right ones. Changing an SLO is a PR to this file approved by the engineering lead and the Product Owner.
