# Definition of Ready

A work item is Ready when the team can start it on day one of the sprint and finish it within the sprint without discovering what it is. Ready is checked in backlog refinement, not in sprint planning.


## Story or bug

| # | Criterion | Check |
|---|---|---|
| 1 | Title states the outcome, not the task | "Operator sees escalation level on incident list", not "Add column" |
| 2 | User/business value in one sentence | `As <role>, I need <capability>, so that <outcome>` or the bug's impact |
| 3 | Acceptance criteria are testable | Given/When/Then, each one maps to at least one test |
| 4 | Contract impact known | Touches [contracts.md](https://github.com/marcelo-roman/incident-ops/blob/main/contracts/contracts.md)? Then a contract PR or ADR is linked and reviewed first |
| 5 | Estimated by the team | Story points, Fibonacci; anything above 8 is split |
| 6 | Dependencies named and unblocked | Linked with `Predecessor`; external ones confirmed with a date in the [risk log](../leadership/risk-and-dependency-log.md) |
| 7 | Non-functional requirements stated | Latency, telemetry, security, data retention where relevant |
| 8 | UX available when there is UI | Link to mock or screenshot; empty, loading and error states included |
| 9 | Area path, iteration, parent Feature and tags set | See [ado-hygiene.md](ado-hygiene.md) |
| 10 | Bugs: reproduction steps, expected vs actual, environment, severity | Logs or KQL link attached |

## Feature

- Problem statement and success metric (for example "MTTA for Sev2 under 20 minutes").
- Stories identified for at least the first sprint, the rest sketched.
- Architecture review done if it adds a component, a contract change or a new Azure resource; ADR linked.
- Rollout plan: feature flag, migration, backfill.
- Owner on Product and Engineering named.

## Not ready: what to do

A story that fails any criterion stays out of the sprint. The person who found the gap owns getting the missing piece, with a due date before the next refinement. Exceptions: Sev1/Sev2 follow-ups and security fixes enter the sprint immediately and are refined in-flight.

## Refinement cadence

- Twice a sprint, 45 minutes, Product Owner, lead and two rotating engineers; the rest of the team reviews async.
- Target: 1.5 sprints of Ready work at the top of the backlog at all times.
- Measured: percentage of sprint-planned items that met DoR at planning (target ≥ 90%).
