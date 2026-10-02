# Definition of Done

Done means running in production, observable, and nothing left for someone else to finish. A work item that misses any line below is not Done and does not count toward velocity or say/do.

## Code

- [ ] Merged to `main` through a pull request with one approval ([code review guidelines](code-review-guidelines.md)).
- [ ] Follows the [conventions](https://github.com/marcelo-roman/incident-ops/blob/main/contracts/contracts.md#conventions): English, no comments, early return, one responsibility per type, domain rules in the domain layer.
- [ ] No TODOs, commented-out code or feature flags left without a removal work item.

## Quality

- [ ] All [quality gates](quality-gates.md) green: build, lint, tests, coverage floor, image scan.
- [ ] Each acceptance criterion has an automated test; bug fixes include a test that failed before the fix.
- [ ] Contract changes have consumer-side tests updated in every affected module.

## Operability

- [ ] New endpoints, consumers and functions emit traces and structured logs with `incidentId` where applicable.
- [ ] New failure modes have an alert or are covered by an existing one; runbook updated if the response differs.
- [ ] Configuration is in environment variables and Bicep; secrets only in GitHub environment or repository secrets, injected at deploy time.
- [ ] Health checks still reflect readiness.

## Documentation

- [ ] Module README updated when usage, configuration or endpoints changed.
- [ ] ADR written for decisions in scope of [docs/adr](../adr/index.md).
- [ ] OpenAPI description accurate (summaries, response codes, problem details).

## Delivery

- [ ] Deployed to production through the GitHub Actions deploy workflow; no manual changes in the portal.
- [ ] [Release readiness checklist](release-readiness-checklist.md) complete for anything user-visible or touching data.
- [ ] Verified in production by the author (smoke test or dashboard) and noted on the work item.
- [ ] Product Owner accepted the story.

## Bugs and incident follow-ups

- [ ] Root cause stated on the work item, not just the fix.
- [ ] Linked to the incident (`INC-####`) and postmortem when it came from one.
- [ ] Recurrence check: a query or alert that would catch it again.
