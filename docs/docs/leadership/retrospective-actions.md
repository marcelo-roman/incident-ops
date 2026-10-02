# Retrospective actions

Retros and postmortems are only worth the actions that get closed. This page defines how actions are captured, tracked and closed, and how follow-through is measured.

## Sources

| Source | Cadence | Typical actions |
| --- | --- | --- |
| Sprint retrospective | every 2 weeks | process, collaboration, tooling |
| Postmortem | per Sev1/Sev2 within 5 business days | prevent, detect, mitigate, process ([template](../operations/postmortem-template.md)) |
| Ops review | weekly | alert tuning, runbook gaps, toil automation |
| Quarterly review | quarterly | allocation, standards, team structure |

## Rules for an action

- **Specific and verifiable**: "Add p95 latency alert on payments-gateway at 2 s" — not "improve monitoring".
- **One owner**, a person, not a team.
- **Due date** within 2 sprints for postmortem actions, by next retro for retro actions.
- **A work item** in Azure Boards tagged `incident-followup` or `retro-action`, sized like any other item and planned into a sprint from the KTLO allocation.
- At most 3 actions per retro. Better 2 that close than 8 that rot.

## Tracking table

Kept as a shared query in Azure Boards; this is the view reviewed in each retro.

| ID | Source | Action | Type | Owner | Created | Due | Status | Evidence of closure |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A-031 | INC-1187 | Size processor connection limit from Little's law at 2× peak | prevent | D. Okafor | 2026-09-19 | 2026-09-24 | Done | PR #412, load test report |
| A-032 | INC-1187 | Alert on payments-gateway p95 > 2 s for 5 min | detect | A. Ribeiro | 2026-09-19 | 2026-09-24 | Done | rule merged, fired in test |
| A-033 | INC-1187 | Canary analysis compares latency at matching traffic | mitigate | P. Nair | 2026-09-19 | 2026-10-15 | In progress | — |
| A-034 | Retro S19 | PR template asks "does this change a limit, timeout or pool?" | process | M. Roman | 2026-09-26 | 2026-10-09 | Done | template updated in all repos |
| A-035 | Retro S19 | Rotate review assignments so every engineer reviews infra once a sprint | process | M. Roman | 2026-09-26 | 2026-10-09 | Open | — |

Statuses: Open → In progress → Done (with evidence) or Dropped (with reason and who agreed).

## Ritual

1. Every retro opens with 5 minutes on the previous retro's actions: done, in progress, dropped.
2. Overdue actions get a new date once; overdue twice means drop or escalate, explicitly.
3. Postmortem actions are reviewed in the weekly ops review until closed.
4. "Done" requires evidence: a PR, a dashboard, a changed template. The check is done by someone other than the owner.

## Metrics

| Metric | Target | Reported in |
| --- | --- | --- |
| Actions closed by due date | ≥ 85% | monthly ops report, [stakeholder update](stakeholder-updates.md) |
| Median age of open actions | < 21 days | retro |
| Recurrence: incidents linked to a cluster that already had a closed action | 0 | [KTLO metrics](../operations/ktlo-metrics.md) |
| Actions dropped | tracked, no target; a high rate means actions are not specific enough | retro |
