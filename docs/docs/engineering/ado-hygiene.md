# Azure DevOps hygiene

Rules that keep Azure Boards trustworthy enough to plan with. If the board does not match reality, sprint metrics are fiction.

## Work item hierarchy

| Level | Type | Size | Owner |
| --- | --- | --- | --- |
| 1 | Epic | a quarter or more; one business outcome | Product Owner |
| 2 | Feature | 1–3 sprints; independently releasable | Product Owner + lead |
| 3 | User Story / Bug | ≤ 8 points; fits in one sprint | team |
| 4 | Task | ≤ 1 day; optional, for the team's own tracking | engineer |

Rules: every Story and Bug has a parent Feature; every Feature has a parent Epic. KTLO work hangs under a standing Epic per quarter, `KTLO 2026-Q4`, with Features per service (`KTLO checkout`, `KTLO payments-gateway`, ...), so it is visible in the same roadmap as feature work. Incident follow-ups are Bugs or Stories linked to the incident number in the title (`[INC-1042] Add pool exhaustion alert`).

## States

| State | Meaning | Entry rule |
| --- | --- | --- |
| New | captured, not refined | anyone |
| Ready | meets [DoR](definition-of-ready.md) | refined, estimated |
| Active | someone is working on it now | assigned; WIP ≤ 2 per engineer |
| In Review | PR open | PR linked |
| Resolved | merged and deployed, awaiting acceptance | deployment run linked |
| Closed | meets [DoD](definition-of-done.md), accepted | PO acceptance |
| Removed | will not do | reason in discussion |

Items do not skip states backward silently: moving Active → Ready requires a discussion comment.

## Area and iteration paths

```text
Area:       IncidentOps
            IncidentOps\API
            IncidentOps\Web
            IncidentOps\Functions
            IncidentOps\Insights
            IncidentOps\Platform        (infra, pipelines, standards)

Iteration:  IncidentOps\2026\Q4\Sprint 20   (2 weeks, Monday to Friday of week 2)
```

Area path = owning component. Iteration path = sprint committed in. Unplanned work entering mid-sprint keeps the sprint iteration and gets the `unplanned` tag.

## Tags

| Tag | Use | Feeds |
| --- | --- | --- |
| `ktlo` | keep-the-lights-on: maintenance, upgrades, operational fixes | KTLO vs feature split |
| `toil` | manual, repetitive, automatable operational work | toil % |
| `tech-debt` | deliberate improvement of code or architecture | debt allocation |
| `incident-followup` | action item from a postmortem | [retrospective actions](../leadership/retrospective-actions.md) |
| `retro-action` | action item from a sprint retrospective | [retrospective actions](../leadership/retrospective-actions.md) |
| `risk`, `dependency` | item tracked in the [risk and dependency log](../leadership/risk-and-dependency-log.md) | weekly risk review |
| `unplanned` | entered after sprint start | say/do, interrupt rate |
| `security` | vulnerability or hardening | compliance reporting |
| `blocked` | waiting on someone outside the team; reason in discussion | standup, risk log |

No other tags without a team decision; free-form tags make queries useless.

## Required fields

| Field | Story/Bug | Feature |
| --- | --- | --- |
| Area path, iteration path | ✔ | ✔ |
| Parent | ✔ | ✔ |
| Story points | ✔ | — |
| Acceptance criteria | ✔ | ✔ (success metric) |
| Severity (Bug) | ✔ | — |
| Target date | — | ✔ |
| Value area (Business / Architectural) | ✔ | ✔ |

## Backlog readiness

- Target: 1.5 sprints of Ready stories ranked at the top of the backlog.
- No item older than 90 days in New: weekly sweep closes or refines it.
- Bugs triaged within 2 business days of creation: severity, area, priority set.

## Capacity planning

Capacity is set per sprint in the Capacity tab before planning.

| Input | Value |
| --- | --- |
| Sprint length | 10 working days |
| Focus hours per engineer per day | 6 |
| On-call primary | 50% capacity that week (interrupt-driven) |
| On-call secondary | 80% capacity |
| PTO and holidays | from the team calendar |
| Allocation | 60% features, 25% KTLO, 15% tech debt (see [KTLO vs roadmap](../leadership/ktlo-vs-roadmap.md)) |

Plan to 80% of computed capacity. The remaining 20% absorbs unplanned work; if it is not used, pull from the top of the Ready backlog.

Worked example, 6 engineers, one with 3 days PTO, one primary and one secondary on call:

```text
Base               6 × 10 days × 6 h            = 360 h
PTO                3 days × 6 h                  = −18 h
On-call primary    1 × 10 × 6 × 50%              = −30 h
On-call secondary  1 × 10 × 6 × 20%              = −12 h
Available                                          300 h
Plan to 80%                                        240 h
  Features 60%   144 h   KTLO 25%   60 h   Debt 15%   36 h
```

## Sprint predictability metrics

| Metric | Definition | Target | Source |
| --- | --- | --- | --- |
| Say/do ratio | points Closed in sprint that were committed at planning ÷ points committed | 80–95% (above 95% suggests sandbagging) | Sprint burndown, committed scope snapshot |
| Velocity stability | coefficient of variation of the last 6 sprints' completed points | < 20% | Velocity report |
| Unplanned rate | points tagged `unplanned` ÷ points completed | < 20% | query below |
| Carry-over | items moved to next sprint ÷ items committed | < 15% | iteration history |
| Cycle time | Active → Closed, p50 and p85 | p85 ≤ 5 days for stories | Cycle time widget |
| WIP | Active items per engineer | ≤ 2 | board |
| Allocation actual vs planned | completed points by tag group ÷ completed points | within ±5 pp of plan | query below |

Reported in the sprint review and in the [weekly stakeholder update](../leadership/stakeholder-updates.md). A say/do below 80% two sprints running triggers a retro topic on planning, not a push to work harder.

## Weekly hygiene sweep

15 minutes, lead and Scrum Master, every Thursday:

1. Active items without an update for 3 days.
2. Items in In Review with no linked PR, or Resolved without a deployment run.
3. Stories without parent, area path or estimate.
4. `blocked` items: blocker still valid? owner? date?
5. Bugs untriaged older than 2 business days.
6. New items older than 90 days.

## Queries

WIQL for the shared queries the sweep and metrics use.

Orphan stories and bugs:

```sql
SELECT [System.Id], [System.Title], [System.State]
FROM WorkItemLinks
WHERE ([Source].[System.TeamProject] = @project
  AND [Source].[System.WorkItemType] IN ('User Story', 'Bug')
  AND [Source].[System.State] <> 'Removed')
  AND ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Reverse')
  AND ([Target].[System.WorkItemType] = 'Feature')
MODE (DoesNotContain)
```

Unplanned work in the current sprint:

```sql
SELECT [System.Id], [System.Title], [Microsoft.VSTS.Scheduling.StoryPoints]
FROM WorkItems
WHERE [System.TeamProject] = @project
  AND [System.IterationPath] = @currentIteration('[IncidentOps]\IncidentOps Team')
  AND [System.Tags] CONTAINS 'unplanned'
```

Allocation split completed this sprint:

```sql
SELECT [System.Id], [System.Tags], [Microsoft.VSTS.Scheduling.StoryPoints]
FROM WorkItems
WHERE [System.TeamProject] = @project
  AND [System.IterationPath] = @currentIteration('[IncidentOps]\IncidentOps Team')
  AND [System.State] = 'Closed'
  AND [System.WorkItemType] IN ('User Story', 'Bug')
```

Group by tag (`ktlo`, `tech-debt`, none = feature) in the Analytics view or a pivot.
