# Stakeholder updates

A weekly written status for Product Owners, Engineering Owners and leadership. One page, sent every Friday by 15:00 ET, same structure every week so readers know where to look.

## Rules

- Lead with what changed and what needs a decision. Nobody reads to the bottom.
- Status colors mean something specific (below), not a mood.
- Every risk has an owner and a date; every blocker says who can unblock it.
- Numbers over adjectives: "say/do 86%" instead of "good sprint".
- Bad news goes out the same day it is known, not in Friday's update.

| Status | Meaning |
| --- | --- |
| Green: on track | committed dates hold with current scope and capacity |
| Amber: at risk | a date or scope is at risk; mitigation in progress; no decision needed yet |
| Red: off track | a date or scope will be missed without a decision from the reader |

## Template

```markdown
# Incident Ops — weekly update, week of {yyyy-mm-dd}

**Overall: {Green/Amber/Red}** {one sentence why}

## Decisions needed
| # | Decision | Options | Recommendation | Needed by | From |
|---|---|---|---|---|---|

## Delivery
| Feature | Status | This week | Next week | Target date |
|---|---|---|---|---|

Sprint {n}: committed {x} pts, done {y} pts, say/do {y/x}%. Unplanned {u}%.
Allocation actual: features {f}%, KTLO {k}%, tech debt {d}% (plan 60/25/15).

## Operations
- Incidents: Sev1 {n}, Sev2 {n}, Sev3/4 {n}. SLA compliance 30d {x}%. MTTA {m} min, MTTR {h} h.
- Notable: {INC-#### one line each, postmortem link}
- On-call load: {pages, after-hours pages}

## Risks
| Risk | Impact | Likelihood | Owner | Mitigation | Due |
|---|---|---|---|---|---|

## Blockers
| Blocker | Since | Who can unblock | Ask |
|---|---|---|---|

## Done this week
- {shipped items, one line each, linked}
```

## Example

```markdown
# Incident Ops — weekly update, week of 2026-09-28

**Overall: Amber** Alert ingestion ships on time; Azure Monitor path slips one week on a dependency.

## Decisions needed
| # | Decision | Options | Recommendation | Needed by | From |
|---|---|---|---|---|---|
| 1 | Pager receiver for ApiDown outside the API | (a) Action Group SMS only, (b) paging tool integration (≈ $40/user/month) | (a) now, revisit after 1 quarter of data | 2026-10-07 | Head of Engineering |

## Delivery
| Feature | Status | This week | Next week | Target date |
|---|---|---|---|---|
| Alertmanager ingestion + dedupe | Green | fingerprint dedupe, auto-mitigate merged | load test 500 alerts/min | 2026-10-09 |
| Azure Monitor ingestion | Amber | schema mapping done | waiting on Action Group permissions (AB#2402) | 2026-10-16 (was 10-09) |
| RCA drafts in console | Green | prompt and output schema reviewed | UI behind flag | 2026-10-23 |

Sprint 20: committed 42 pts, done 36 pts, say/do 86%. Unplanned 14%.
Allocation actual: features 58%, KTLO 29%, tech debt 13% (plan 60/25/15).

## Operations
- Incidents: Sev1 0, Sev2 2, Sev3/4 7. SLA compliance 30d 96.1%. MTTA 11 min, MTTR 3.4 h.
- Notable: INC-1203 Sev2 dead letters on sla-checks after API key rotation; 2 escalations late by 40 min; postmortem 2026-10-06.
- On-call load: 9 pages, 1 after hours.

## Risks
| Risk | Impact | Likelihood | Owner | Mitigation | Due |
|---|---|---|---|---|---|
| SignalR Free tier message quota during incident spikes | live updates stop | medium | M. Roman | upgrade path to S1 tested in staging | 2026-10-09 |

## Blockers
| Blocker | Since | Who can unblock | Ask |
|---|---|---|---|
| Action Group contributor role in subscription | 2026-09-29 | Cloud platform team | approve AB#2402 |

## Done this week
- Fingerprint dedupe for Alertmanager alerts
- Runbook: dead letters, with replay steps
- EF Core 8.0.10 upgrade (KTLO)
```

## Audience variants

| Audience | Length | Keep | Drop |
| --- | --- | --- | --- |
| Executives | 5 lines | overall status, decisions, top risk, one operations number | delivery table detail, done list |
| Product Owners | full template | everything | — |
| Engineering team | full template + links to boards | everything, plus carry-over reasons | — |
