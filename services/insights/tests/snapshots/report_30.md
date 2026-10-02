# KTLO report: 2026-08-29 to 2026-09-28

Generated 2026-09-28 00:00 UTC over the trailing 30 days.

## Headline

| Incidents | Detected by monitoring | SLA compliance | Acknowledge SLA | Resolve SLA | MTTA median | MTTA p90 | MTTR median | MTTR p90 |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 65 | 56.9% | 65.6% | 76.6% | 85.2% | 1h 10m | 7h 28m | 18h 17m | 3d 22h 32m |

## Detection

| Source | Incidents | Share | MTTA median | MTTA p90 |
|---|---:|---:|---:|---:|
| Manual | 28 | 43.1% | 2h 39m | 17h 40m |
| Alertmanager | 20 | 30.8% | 40m | 2h 19m |
| AzureMonitor | 17 | 26.2% | 31m | 5h 12m |

## Top recurring issues

| Cluster | Incidents | Services | Example |
|---|---:|---|---|
| acquirer authorization / authorization timeouts / card authorization | 6 | payments-gateway (6) | Card authorization timeouts with acquirer |
| push notifications / android devices / failing android | 6 | notifications (6) | Push notifications failing for Android devices |
| error rate / deployment / rate spike | 5 | checkout (1), identity (1), notifications (1), reporting (1), search (1) | Error rate spike after deployment of search |
| failures invalid / invalid token / login failures | 5 | identity (5) | Login failures with invalid token errors |
| backlog payment / delivery backlog / payment webhook | 4 | payments-gateway (4) | Payment webhook delivery backlog |

## SLA breaches

21 incidents breached at least one SLA target; the 15 most severe are listed.

| Incident | Severity | Service | Status | Breached | Resolve overrun | Assignee |
|---|---|---|---|---|---:|---|
| INC-1391 Duplicate charges reported by customers | Sev1 | payments-gateway | Resolved | acknowledge, resolve | 42m | alice.nguyen |
| INC-1366 Card authorization timeouts with acquirer | Sev1 | payments-gateway | Resolved | acknowledge | n/a | erin.walsh |
| INC-1410 Push notifications failing for Android devices | Sev2 | notifications | Resolved | acknowledge | n/a | bruno.costa |
| INC-1408 Checkout API p99 latency above 800ms | Sev2 | checkout | Resolved | resolve | 56m | carla.mendes |
| INC-1407 Order placement failing with HTTP 500 | Sev2 | checkout | Resolved | acknowledge | n/a | bruno.costa |
| INC-1387 Error rate spike after deployment of checkout | Sev2 | checkout | Resolved | acknowledge | n/a | farah.khan |
| INC-1381 Database connection pool exhausted on checkout | Sev2 | checkout | Resolved | resolve | 31m | farah.khan |
| INC-1372 Card authorization timeouts with acquirer | Sev2 | payments-gateway | Resolved | acknowledge | n/a | erin.walsh |
| INC-1360 Login failures with invalid token errors | Sev2 | identity | Resolved | resolve | 10h 17m | diego.ramos |
| INC-1406 Checkout API p99 latency above 1500ms | Sev3 | checkout | Resolved | resolve | 22h 32m | alice.nguyen |
| INC-1396 Checkout API p99 latency above 3000ms | Sev3 | checkout | Resolved | acknowledge | n/a | bruno.costa |
| INC-1384 Database connection pool exhausted on reporting | Sev3 | reporting | Resolved | acknowledge | n/a | farah.khan |
| INC-1377 MFA SMS codes not delivered | Sev3 | identity | Resolved | acknowledge | n/a | erin.walsh |
| INC-1367 Search results stale after catalog update | Sev3 | search | Resolved | acknowledge | n/a | erin.walsh |
| INC-1362 Push notifications failing for Android devices | Sev3 | notifications | Resolved | resolve | 2d 23h 22m | farah.khan |

## MTTR by service

| Service | Incidents | MTTR median | MTTR p90 | MTTA median | SLA compliance |
|---|---:|---:|---:|---:|---:|
| search | 9 | 1d 12h 49m | 3d 17h 36m | 2h 14m | 71.4% |
| reporting | 7 | 1d 6h 25m | 3d 8h 43m | 2h 41m | 71.4% |
| checkout | 10 | 1d 3h 3m | 3d 22h 44m | 39m | 40.0% |
| notifications | 11 | 22h 4m | 5d 23h 58m | 1h 15m | 66.7% |
| identity | 10 | 12h 4m | 2d 23h 52m | 25m | 70.0% |
| platform | 4 | 11h 24m | 1d 12h 5m | 1h 8m | 100.0% |
| payments-gateway | 14 | 4h 16m | 2d 5h 8m | 41m | 64.3% |

## On-call load

Off-hours means created outside 09:00-18:00 America/New_York on weekdays.

| Assignee | Incidents | Sev1/Sev2 | Off-hours | Escalated |
|---|---:|---:|---:|---:|
| farah.khan | 16 | 5 | 13 | 4 |
| bruno.costa | 15 | 3 | 12 | 3 |
| erin.walsh | 13 | 8 | 11 | 4 |
| alice.nguyen | 12 | 5 | 10 | 2 |
| diego.ramos | 6 | 2 | 6 | 2 |
| carla.mendes | 2 | 1 | 1 | 0 |
| unassigned | 1 | 0 | 1 | 0 |
