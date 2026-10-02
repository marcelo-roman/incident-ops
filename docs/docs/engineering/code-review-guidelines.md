# Code review guidelines

Review exists to keep `main` releasable, spread knowledge, and catch what tests do not. It is not a gate to prove seniority.

## Service levels

| Item | Target |
| --- | --- |
| Time to first review | 4 business hours |
| PR size | under 400 changed lines excluding generated files and lockfiles |
| Approvals | 1; 2 for contract, migration, auth or pipeline changes |
| Hotfix review | 30 minutes, any engineer on the rotation |

A PR waiting more than one business day is raised at standup by the author, not left to rot.

## Author responsibilities

- Description: what and why, linked work item (`AB#1234`), how it was tested, screenshots for UI, risks.
- Self-review the diff before requesting review.
- CI green before requesting review.
- Split refactors from behavior changes.
- Merge your own PR after approval; you own the deploy that follows.

## Reviewer checklist

1. **Correctness**: does it do what the acceptance criteria say, including error paths and concurrency?
2. **Contract**: does it match [contracts.md](https://github.com/marcelo-roman/incident-ops/blob/main/contracts/contracts.md)? Is any change additive?
3. **Design**: domain rules in the domain; endpoints and handlers thin; one responsibility per type.
4. **Tests**: do they fail without the change? Do they test behavior rather than implementation?
5. **Operability**: logs with correlation ids, telemetry, alerts, configuration via environment.
6. **Security**: input validation, authorization on writes, no secrets, dependency changes justified.
7. **Readability**: names carry meaning; no comments needed to explain the code.

## Comment conventions

| Prefix | Meaning | Blocks merge |
| --- | --- | --- |
| `blocker:` | correctness, security, contract break | yes |
| `issue:` | should change; author decides now or opens a follow-up item | author's call, with a linked item |
| `suggestion:` | alternative worth considering | no |
| `question:` | reviewer wants to understand | no |
| `nit:` | style beyond the linter | no |
| `praise:` | something done well | no |

Two rounds of back-and-forth on the same point means a 10-minute call, then a summary comment.

## Architecture reviews

Required for: new component or Azure resource, contract change, new dependency on another team, data model change affecting history. Format:

1. Author writes an ADR draft (problem, options, recommendation) as a PR that adds it under `docs/docs/adr/`.
2. Async comments for 2 business days.
3. 30-minute weekly slot to resolve open points; decision recorded in the ADR.

## Coaching through review

- Explain the principle, link the standard, show one example; do not rewrite the PR in comments.
- For engineers in their first 3 months: pair review once a week on one of their PRs.
- Rotate reviewers so every engineer reviews every module at least once a sprint.
- Recurring findings become a lint rule, a template change or a short session, not repeated comments.

## Metrics

Tracked monthly from Azure DevOps analytics: median time to first review, median PR cycle time (open → merge), PR size distribution, review load per engineer. Targets are team goals, not individual performance measures.
