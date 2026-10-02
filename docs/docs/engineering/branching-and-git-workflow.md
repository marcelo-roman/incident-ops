# Branching and git workflow

Trunk-based development with short-lived branches in one repository, [marcelo-roman/incident-ops](https://github.com/marcelo-roman/incident-ops). `main` is always releasable and is what production runs. Every module lives in the same repository, so one pull request can change a contract and the modules that implement it ([ADR 0010](../adr/0010-monorepo-with-path-filtered-pipelines-and-codeowners.md)).

## Branches

| Prefix | Use | Example |
| --- | --- | --- |
| `feature/` | new behavior | `feature/1234-escalation-level-column` |
| `hotfix/` | production fix that cannot wait for the next normal release | `hotfix/1301-sla-check-null-assignee` |
| `chore/` | dependencies, build, docs, refactors without behavior change | `chore/bump-efcore-8-0-10` |

- A branch changes one module when it can. Changes that span modules (a contract change and its consumers) go together in one pull request.

- Branch from `main`, merge into `main`. No long-lived `develop` or release branches.
- Lifetime: target under 2 days, hard limit 5. Larger work is split or merged dark behind a feature flag.
- Work item id in the branch name when one exists.

## Commits

- Imperative subject under 72 characters: `Add escalation level to incident list`.
- Body explains why when it is not obvious from the diff.
- Reference work items with `AB#1234` so Azure Boards links them.
- Rebase on `main` locally to stay current; never rewrite a branch someone else has pulled.

## Pull requests

- Title becomes the squash commit subject; write it for the changelog.
- Template: what, why, how tested, risks, rollback, linked work item.
- Draft PRs are encouraged for early feedback; CI runs on drafts too.
- Squash merge only; branch deleted on merge.

## Path-filtered checks

Each workflow runs only when its paths change. The check names below are the job names GitHub reports.

| Change under | Workflow | Checks |
| --- | --- | --- |
| `services/api/**` | `api.yml` | `Build and test` (`Build and push image`, `Deploy to Azure Container Apps` on `main`) |
| `services/functions/**` | `functions.yml` | `Build and test` (`Deploy to Azure Functions` on `main`) |
| `services/insights/**` | `insights.yml` | `Lint, type check and test`, `Container image` (`Deploy to Azure Container Apps` on `main`) |
| `apps/web/**` | `web.yml` | `Lint, test and build`, `Container image` (`Deploy to Static Web Apps` on `main`) |
| `infra/**` | `infra.yml` | `Bicep build and lint`, `ShellCheck and Azure DevOps samples`, `Validate and what-if` (`Deploy` on `main`) |
| `docs/**` | `docs.yml` | `Build site` (`Deploy to GitHub Pages` on `main`) |
| `contracts/**` | `api.yml`, `functions.yml`, `insights.yml`, `web.yml` on pull requests | every application check above |
| `docker-compose.yml`, `.env.example`, `local/**`, `scripts/**`, any `*.md`, `*.yml`, `*.yaml` or `Dockerfile`, the lint configs, `.github/**` | `platform.yml` | `Compose config`, `Alerting rules and config`, `Markdown, YAML and Dockerfile lint`, `Markdown links and scripts`, `Workflow lint` |
| `.github/workflows/<name>.yml` | that workflow and `platform.yml` | its checks and `Workflow lint` |
| `.github/workflows/deploy-container-app.yml` | `api.yml`, `insights.yml`, `platform.yml` | their checks |

## Code owners

[`.github/CODEOWNERS`](https://github.com/marcelo-roman/incident-ops/blob/main/.github/CODEOWNERS) starts with a catch-all and then assigns an owner per top-level path: `services/api/`, `services/functions/`, `services/insights/`, `apps/web/`, `infra/`, `docs/`, `local/`, `.github/`, and `contracts/` last so its rule takes precedence. With "require review from code owners" on the `main` ruleset, a pull request that touches `contracts/contracts.md`, a workflow or an Azure DevOps sample cannot merge without that owner's approval.

## Required checks per path

GitHub reports a required status check whose workflow was skipped by its path filter as pending, and the pull request cannot merge. Listing every module's checks as required would block every pull request that does not touch all modules. The ruleset therefore enforces what applies to every pull request, and the path table above defines what else must be green:

- The `main` ruleset requires a pull request, code owner review and an up-to-date branch.
- The reviewer approves only when every check that ran on the pull request is green, and when the checks listed above for each changed path are present.
- Auto-merge is not used, so a pending or missing check is visible at merge time.

An always-running aggregator workflow that computes the changed paths and waits for the matching checks would make this mechanical and allow one required check; it is the next step if more contributors join.

## Branch policies

Configured on `main`. The repository lives on GitHub, so the enforcing mechanism is a GitHub ruleset; the right-hand column is the equivalent Azure Repos branch policy for teams hosting code in Azure DevOps.

| Rule | GitHub ruleset on `main` | Azure Repos branch policy |
| --- | --- | --- |
| Reviews | pull request required; 0 approvals while the repository has a single maintainer (GitHub does not let authors approve their own pull requests), 1 approval with dismissal of stale approvals once a second maintainer joins | Minimum reviewers 1; reset votes on new pushes; author cannot approve |
| Code owners | `CODEOWNERS` review required; `contracts/`, `.github/` and `infra/azure-devops/` always need their owner | Automatically included required reviewers on the same paths |
| Status checks | path-filtered workflows, reviewed as described in [required checks per path](#required-checks-per-path); branch must be up to date with `main` | Build validation policy per path filter (one per pipeline in `infra/azure-devops/`); expires when `main` updates |
| Conversations | all resolved before merge | Comment resolution required |
| Merge method | squash only | Limit merge types: squash |
| Work item link | `AB#1234` in PR description (Azure Boards app links it); PR template checkbox | Linked work items required |
| Direct pushes, force pushes, deletion | blocked, no bypass list | Branch locked to PRs; no bypass permission granted |
| Deployments | `production` environment with required reviewers; deploys only from `main` | Environment approvals and checks on `incident-ops-prod` |

## Deploys only from `main`

Deploy jobs run only on a push to `main`. The power workflow, [`power.yml`](https://github.com/marcelo-roman/incident-ops/blob/main/.github/workflows/power.yml), starts and stops the Azure environment on `workflow_dispatch` and powers it down on a daily `schedule`; GitHub runs scheduled workflows on the default branch only, and a manual run on another branch is skipped by its job guard. Three controls make sure that editing a workflow in a pull request cannot deploy or change the power state from another branch:

| Layer | Control |
| --- | --- |
| Workflow | deploy jobs require `github.event_name == 'push' && github.ref == 'refs/heads/main'`; the power job requires `github.ref == 'refs/heads/main'`; pull requests run build, lint and test only |
| GitHub environments | `production`, `power` and `github-pages` accept deployments from `main` only (deployment branch policy); manual power runs use `production` and its required reviewer, the scheduled power down uses `power`, which has no reviewer so the nightly run does not wait for an approval |
| Azure | the OIDC federated credentials trust only `environment:production`, `environment:power` and `ref:refs/heads/main`; a job from a pull request or any other branch cannot obtain a token, so validation and what-if run on `main` right before the deploy |

Azure deploy jobs and the power job also require the repository variable `DEPLOY_ENABLED=true`. Until the Azure resources and the OIDC bootstrap exist, deploys report as skipped instead of failing; setting the variable enables them without a code change.

## Hotfix flow

1. Branch `hotfix/<id>-<slug>` from `main` (which is what runs in production).
2. Smallest possible change plus a regression test.
3. PR with expedited review (30-minute target, any engineer on the rotation).
4. Merge and deploy through the normal workflow (the `production` environment approval still applies) with release checklist section 5 ([release readiness](release-readiness-checklist.md#5-rollout-and-rollback)).
5. Link the hotfix to the incident and the postmortem action item.

## Releases and tags

- Every merge to `main` that touches a deployable module builds its artifact; images are tagged with the commit SHA and `latest`.
- Production deploys are recorded as GitHub deployments on the `production` environment, with the image tag and the approving reviewer.
- Rollback is a redeploy of the previous image tag or a copy of the previous container app revision, never a revert-and-rebuild under pressure.

## What we do not do

- Merge with a failing check, or with a check missing for a path the pull request changes.
- Cherry-pick between release branches (there are none).
- Keep feature flags past two sprints after full rollout; removal is a work item created with the flag.
