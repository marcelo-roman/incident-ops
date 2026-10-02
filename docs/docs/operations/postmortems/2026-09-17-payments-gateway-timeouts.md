# INC-1187: payments-gateway authorization timeouts after connection pool change

Fictional example written against the [postmortem template](../postmortem-template.md). Services and numbers are from the Incident Ops demo domain.

| | |
|---|---|
| Severity | Sev1 |
| Service | `payments-gateway` (Tier1), impact on `checkout` |
| Date | 2026-09-17 |
| Duration | detection → mitigation 0:45; → resolution 3:33 |
| SLA | ack Met (9 min of 15), resolve Met (3 h 33 min of 4 h) |
| Incident commander | Ana Ribeiro |
| Postmortem owner | Daniel Okafor |
| Status | Published |


## Summary

A configuration change deployed at 14:05 capped outbound HTTP connections from `payments-gateway` to the card processor at 10 per replica. Under the midday peak, processor latency rose from a p50 of 300 ms to 1.2 s and requests queued for a connection until they hit the 10 s client timeout. 31% of authorization requests failed for 50 minutes. Rolling back to the previous Container Apps revision restored service at 16:52. The root cause was a pool size chosen without a capacity calculation, in a change classified as low risk.

## Impact

- Authorization requests failed: 109 400 of 352 800 (31%) between 16:02 and 16:52.
- Orders that failed checkout after client retries: 24 870.
- Customers saw "payment could not be processed"; no duplicate charges (idempotency keys held).
- `payments-gateway` availability SLO (99.9% monthly): about 70% of the monthly error budget consumed (budget ≈ 155 000 failed requests at 60 req/s average).

## Timeline (UTC)

| Time | Event |
|---|---|
| 14:05 | PR "chore: consolidate HTTP client settings" deployed; sets `MaxConnectionsPerServer = 10` on the processor client (previously unbounded). Canary at 10% for 15 min passes at ~25 req/s total |
| 14:20 | Revision promoted to 100% |
| 15:40 | Traffic ramps toward peak; p95 authorization latency climbs from 0.9 s to 3.5 s. No alert (threshold 5 s) |
| 16:02 | First timeouts (`TaskCanceledException` after 10 s) |
| 16:07 | Alert "payments-gateway 5xx > 5% for 5 min" fires; INC-1187 opened automatically as Sev2 |
| 16:16 | Primary on call acknowledges (9 min) |
| 16:20 | Raised to Sev1 (checkout failing for > 25% of requests); IC Ana Ribeiro, comms lead and scribe assigned; bridge opened |
| 16:24 | First stakeholder update sent |
| 16:31 | Dependency telemetry shows processor latency up but processor 5xx flat; requests spend most time before the first byte is sent |
| 16:38 | Scribe links the 14:05 deployment from the `production` environment history; IC decides to roll back |
| 16:48 | Traffic shifted to previous revision |
| 16:52 | Error rate under 0.5%; incident marked Mitigated |
| 17:22 | 30 min stable; second stakeholder update with mitigation |
| 18:30 | Load test in staging reproduces queueing at 10 connections and 120 req/s with 1.2 s upstream latency |
| 19:40 | Root cause recorded; incident Resolved |

Time to detect: 5 min from first error, 2 h 2 min from the change. Time to acknowledge: 9 min. Time to mitigate: 45 min from alert.

## Root cause

Concurrent connections needed = arrival rate × latency (Little's law). At peak, 120 req/s × 1.2 s = 144 concurrent requests across 6 replicas, or 24 per replica. The cap of 10 per replica allowed 60 in total. The remaining requests waited in the handler's connection queue; queue wait counted against the 10 s timeout, so under load most queued requests timed out without ever reaching the processor.

The cap was introduced to protect the processor's documented limit of 200 concurrent connections per merchant. The value 10 was copied from another client's settings and not derived from traffic or latency.

## Contributing factors

- **Risk classification.** The change was labeled `chore` and reviewed as a refactor. A connection limit on a Tier1 dependency is a capacity change.
- **Canary at low traffic.** The 15-minute canary ran at ~25 req/s, where 60 connections were sufficient. Canary analysis did not look at the latency trend at peak.
- **No queueing telemetry.** There was no metric for time waiting for a connection; dependency duration hid it inside the total call time.
- **Timeout budget inverted.** `payments-gateway` waits 10 s on the processor while `checkout` gives up on `payments-gateway` after 8 s and retries once. Retries added load to the queue during the incident.
- **Latency alert too loose.** p95 at 3.5 s for 20 minutes did not alert (threshold 5 s); the first signal was errors.

## Detection

The 5xx alert worked as designed and fired 5 minutes after the first error. A p95 latency alert at 2 s would have fired around 15:50, about 12 minutes before customer-facing errors. The incident was opened as Sev2 by the alert rule and raised manually; the rule's severity mapping did not account for checkout impact.

## Response

What went well:
- Acknowledged in 9 minutes; roles assigned within 4 minutes of Sev1 declaration.
- The deployment history on the `production` environment made the suspect change visible in one click.
- Rollback to the previous revision took 10 minutes end to end and needed no schema change.
- Stakeholder updates went out on the 30-minute cadence without being asked.

What was hard:
- Two people investigated the processor's status page for 10 minutes in parallel, unaware of each other; the scribe's timeline was ahead of the bridge.
- The runbook for high error rate did not mention recent configuration changes, only code deploys.

Where we got lucky:
- Idempotency keys prevented duplicate charges when `checkout` retried.
- The peak was the regular midday peak, not a promotion day at 3× traffic.

## Action items

| # | Action | Type | Owner | Due | Work item |
|---|---|---|---|---|---|
| 1 | Size the processor connection limit from Little's law at 2× peak (≥ 50 per replica, ≤ 200 total) and make it configuration per environment | prevent | Daniel Okafor | 2026-09-24 | AB#2311 |
| 2 | Invert timeout budget: processor client 5 s, `checkout` → `payments-gateway` 8 s, retries only on idempotent calls with jitter | prevent | Priya Nair | 2026-10-01 | AB#2312 |
| 3 | Add connection-queue wait metric and a dashboard tile for the processor client | detect | Daniel Okafor | 2026-10-01 | AB#2313 |
| 4 | Alert on `payments-gateway` p95 > 2 s for 5 min (Sev2) | detect | Ana Ribeiro | 2026-09-24 | AB#2314 |
| 5 | Map "checkout success rate < 90%" alert directly to Sev1 | detect | Ana Ribeiro | 2026-09-24 | AB#2315 |
| 6 | Release checklist: changes to timeouts, pool sizes, retry or rate limits are capacity changes and need a load test result in the PR | process | Marcelo Roman | 2026-09-24 | AB#2316 |
| 7 | Add "recent configuration changes" step to the [high error rate runbook](../runbooks/api-high-error-rate.md) | process | Ana Ribeiro | 2026-09-22 | AB#2317 |
| 8 | Canary analysis compares p95 latency at matching traffic level, not only error rate | mitigate | Priya Nair | 2026-10-15 | AB#2318 |

## Lessons

Any setting that limits concurrency (pool sizes, semaphores, rate limits, timeouts) is a capacity decision and must be derived from arrival rate and latency at peak, not copied. Canary analysis at low traffic does not validate capacity changes.
