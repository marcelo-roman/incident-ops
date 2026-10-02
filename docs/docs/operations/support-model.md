# Support model

Who handles what, when, and how work moves between tiers.


## Tiers

| Tier | Who | Handles | Escalates to L(n+1) when |
|---|---|---|---|
| L1 | Service desk / operations analysts | Intake, validation, known-issue matching, runbook steps marked L1, customer comms | not resolved by a runbook within 30 min, or Sev1/Sev2 suspected |
| L2 | Engineer on call (primary, then secondary) | Triage, mitigation, rollbacks, config changes, runbooks marked L2, opening incidents | root cause needs code change or design knowledge; mitigation not effective within 60 min (Sev1) / 2 h (Sev2) |
| L3 | Owning team's engineers, engineering lead, platform/vendor support | Code fixes, hotfixes, architecture-level mitigation, vendor cases | — |

L1 never waits on L2 to open an incident: anyone can open one. Severity can be raised by anyone and lowered only by the incident commander.

## Hours

| Window | Coverage |
|---|---|
| Business hours: Mon–Fri 10:30–18:30 America/New_York | L1 staffed; L2 primary on call responds within SLA; L3 owning team available |
| Outside business hours, weekends, holidays | Sev1/Sev2 page L2 primary → secondary → engineering lead. Sev3/Sev4 queue for next business day |

The 10:30 start aligns with the rotation handover so the incoming primary starts the week with a full day of team overlap.

## On-call rotation

Matches the [contract](https://github.com/marcelo-roman/incident-ops/blob/main/contracts/contracts.md#on-call-and-escalation) and `GET /api/oncall/current`.

| Parameter | Value |
|---|---|
| Rotation size | 6 engineers |
| Shift | 1 week, Monday 10:30 to Monday 10:30 America/New_York |
| Frequency | primary 1 week in 6; secondary the week before being primary |
| Escalation | level 1 primary → level 2 secondary → level 3 engineering lead |
| Response after page | acknowledge within the severity's ack window ([severity-and-sla.md](severity-and-sla.md)) |
| Swaps | allowed; the swapping engineers update the schedule and post in the team channel 24 h ahead |
| Compensation | time off in lieu: 0.5 day per week as primary, plus hours worked outside business hours |
| Capacity | primary planned at 50% sprint capacity, secondary at 80% ([ADO hygiene](../engineering/ado-hygiene.md#capacity-planning)) |

Being secondary the week before primary gives every engineer context on what is ongoing before taking the pager.

New engineers shadow two rotations (as a third, not paged) before joining.

## Paging

- Sev1/Sev2 `incident.triggered` and `incident.escalated` events go through `NotifyOnCall` → Logic App → Teams/Slack webhook and email, and to the paging tool's phone/SMS integration.
- An unacknowledged page escalates automatically per [SLA escalation flow](../architecture/sla-escalation.md).
- Sev3/Sev4 do not page; they appear in the console and the L2 queue.
- Alerts from Prometheus/Alertmanager and Azure Monitor open incidents through the API, deduplicated by fingerprint, with severity mapped from the rule ([alerting](alerting.md)).

## Handoffs

Every Monday at 10:30, 15 minutes, outgoing and incoming primary:

1. Open incidents and their state.
2. Mitigations in place that need follow-up (feature flags off, scaled-up resources, manual workarounds).
3. Noisy alerts seen during the week → work items tagged `toil`.
4. Upcoming releases and changes that touch production.

The outgoing primary posts the handoff summary in the team channel.

## Intake channels

| Channel | Use | Response |
|---|---|---|
| Console / API `POST /api/incidents` | any production issue | per severity SLA |
| Azure Monitor alerts | automated detection | opens incident automatically |
| Team channel | questions, non-urgent requests | best effort, business hours |
| Azure Boards bug | defects not affecting production now | triaged within 2 business days |

## Support SLAs

Acknowledge and resolve targets per severity are in [severity-and-sla.md](severity-and-sla.md). Support-level targets on top of those:

| Measure | Target |
|---|---|
| L1 → L2 handoff completeness (repro, scope, timeline in the incident) | 95% of escalated incidents |
| First stakeholder update after Sev1 declared | 15 minutes |
| Postmortem published for Sev1/Sev2 | 5 business days |
| Postmortem action items closed by due date | 85% |

## On-call health

Reviewed monthly; numbers come from the API and Insights ([KTLO metrics](ktlo-metrics.md)).

| Signal | Threshold that triggers action |
|---|---|
| Pages outside business hours per week | > 2 |
| Pages per primary shift | > 10 |
| Non-actionable pages (closed as noise) | > 10% |
| Same engineer paged at night in consecutive shifts | any |

Action means alert tuning or automation work in the next sprint, taken from the KTLO allocation.
