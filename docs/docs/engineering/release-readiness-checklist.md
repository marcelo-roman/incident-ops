# Release readiness checklist

Completed by the release owner and linked in the pull request (or the release work item) before a reviewer approves the `production` environment deployment in GitHub Actions. Small changes behind the standard pipeline (no schema, no contract, no new resource) only need section 1 and 5.


## 1. Scope and approval

- [ ] Work items in the release are Done per [DoD](definition-of-done.md) and linked to the run.
- [ ] Changelog generated from merged PR titles and reviewed.
- [ ] No open Sev1/Sev2 on an affected service.
- [ ] Release window respects the freeze rules below.

## 2. Data and contracts

- [ ] EF Core migrations are backward compatible with the currently running version (expand, migrate, contract over two releases).
- [ ] Migration tested against a copy of production schema; duration measured: ____ s.
- [ ] Contract changes are additive, or every consumer is already deployed with support.
- [ ] Service Bus entity changes (new subscription, filter change) applied by Bicep before the publisher change.

## 3. Operations

- [ ] Dashboards and alerts cover the change; alert thresholds reviewed.
- [ ] Runbook updated if response steps changed.
- [ ] On-call primary and secondary know the release is happening and what it touches.
- [ ] Load or soak test run if the change affects a hot path (`GET /api/incidents`, event publishing).

## 4. Communication

- [ ] Stakeholders notified 1 business day ahead for user-visible changes.
- [ ] Release notes posted after deploy.

## 5. Rollout and rollback

- [ ] Rollout: the reusable `deploy-container-app.yml` workflow creates a new revision, smoke tests the health URL and rolls back to the previous revision automatically on failure. For risky changes, shift 10% traffic for 15 minutes before 100%.
- [ ] Promotion criteria: 5xx rate and p95 latency on the new revision within 10% of the old one ([SLO](../operations/slo.md)).
- [ ] Rollback path written down and verified: activate previous revision (API, Insights; automatic on failed smoke test), re-run the deploy workflow for the previous commit (Functions, web).
- [ ] Rollback does not require a schema rollback (see 2).
- [ ] Feature flags in place for behavior that cannot be rolled back by redeploy.

## 6. Go/no-go

| Role | Name | Go |
|---|---|---|
| Release owner | | ☐ |
| Engineering lead | | ☐ |
| Product Owner (user-visible changes) | | ☐ |
| On-call primary | | ☐ |

## Release freeze rules

- No production releases Friday after 14:00 EST or the day before a holiday, except hotfixes.
- No releases while the error budget for the affected SLO is exhausted, except reliability fixes ([error budget policy](../operations/slo.md#error-budget-policy)).
- Hotfixes skip sections 3–4 but not 5.
