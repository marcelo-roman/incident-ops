# Risk and dependency log

A single table per program, reviewed weekly. Lives in the team wiki or as a shared query over work items tagged `risk` / `dependency`; this page is the format.

## Risk scoring

| Score | Likelihood | Impact |
|---|---|---|
| 1 | unlikely (< 10%) | negligible; absorbed within a sprint |
| 2 | possible (10–40%) | minor; one feature slips < 1 week |
| 3 | likely (40–70%) | moderate; milestone slips or SLA at risk |
| 4 | almost certain (> 70%) | major; commitment missed, customer impact |

Exposure = likelihood × impact. ≥ 9 goes into the weekly [stakeholder update](stakeholder-updates.md); ≥ 12 needs a decision owner above the team.

Responses: **avoid** (change the plan), **mitigate** (reduce likelihood or impact), **transfer** (another team or vendor owns it), **accept** (record who accepted it and until when).

## Risk log template

| ID | Raised | Risk (cause → event → effect) | L | I | Exposure | Response | Mitigation / trigger | Owner | Review by | Status |
|---|---|---|---|---|---|---|---|---|---|---|
| R-001 | | | | | | | | | | Open |

## Dependency log template

| ID | Raised | We need | From (team / vendor) | Needed by | Confirmed date | Contact | Impact if late | Status | Work item |
|---|---|---|---|---|---|---|---|---|---|
| D-001 | | | | | | | | Requested | AB# |

Dependency statuses: Requested → Confirmed (date agreed) → Delivered, or At risk (confirmed date missed or unconfirmed within 5 business days of request).

## Example entries

| ID | Raised | Risk | L | I | Exp. | Response | Mitigation / trigger | Owner | Review by | Status |
|---|---|---|---|---|---|---|---|---|---|---|
| R-014 | 2026-09-22 | SignalR Free tier quota (20 000 msg/day) → exhausted during a large incident → live updates stop | 2 | 3 | 6 | mitigate | Client refetch fallback in place; Bicep param for S1 tested; trigger: > 15 000 msg/day | M. Roman | 2026-10-09 | Open |
| R-015 | 2026-09-29 | API key shared by Functions and alert sources → rotated on one side only → escalations and alert ingestion fail | 3 | 3 | 9 | mitigate | Rotation runbook updates the deployment secret (Container Apps secret, Function app setting, Action Group URL) and the Alertmanager file together; alert on 401s from Functions | A. Ribeiro | 2026-10-06 | Open |
| R-016 | 2026-09-29 | Serverless SQL auto-pause → first request after idle takes seconds → availability test flaps at night | 3 | 1 | 3 | accept | Accepted by EO until traffic justifies provisioned tier; review quarterly | D. Okafor | 2026-12-15 | Accepted |

| ID | Raised | We need | From | Needed by | Confirmed | Impact if late | Status | Work item |
|---|---|---|---|---|---|---|---|---|
| D-007 | 2026-09-29 | Contributor on Action Groups in production subscription | Cloud platform team | 2026-10-06 | — | Azure Monitor ingestion slips 1 week | At risk | AB#2402 |
| D-008 | 2026-09-15 | Teams incoming webhook for #inc-major channel | Collaboration admins | 2026-09-26 | 2026-09-24 | Pages only via email | Delivered | AB#2350 |

## Review

- Weekly, 15 minutes in the planning or hygiene slot: update status, scores and dates; close what no longer applies.
- A dependency unconfirmed after 5 business days becomes a risk and is escalated by the lead to the other team's lead.
- Every new Feature in refinement is checked for dependencies before it is Ready ([DoR](../engineering/definition-of-ready.md)).
