# Quality gates

What CI enforces, per workflow. CI is GitHub Actions: one workflow per module in [`.github/workflows/`](https://github.com/marcelo-roman/incident-ops/tree/main/.github/workflows), each filtered by path, so a pull request runs the gates of the modules it changes. A failed gate blocks merge ([branching](branching-and-git-workflow.md#required-checks-per-path)).

[`infra/azure-devops/`](https://github.com/marcelo-roman/incident-ops/tree/main/infra/azure-devops) holds the Azure DevOps equivalent of each deployable's pipeline, with the same stages and path filters. They are samples and are not wired to a project.

## Common to every module

| Gate | Threshold |
|---|---|
| Build | zero errors; .NET builds treat warnings as errors (`TreatWarningsAsErrors`, nullable reference types, `AnalysisLevel` `latest-recommended`) |
| Lint and format | zero findings |
| Tests | all pass |
| Architecture rules | all pass (NetArchTest, import-linter, `eslint-plugin-boundaries`) |
| Coverage floor | per module, below; enforced by the build, not by review |

## Per workflow

| Workflow | Runs when | Build | Lint and format | Tests and coverage | Architecture | Other |
|---|---|---|---|---|---|---|
| `api.yml` | `services/api/**`, `contracts/**` (PR), the workflow, `deploy-container-app.yml` | `dotnet build -c Release` | `dotnet format --verify-no-changes` | 204 tests incl. Testcontainers SQL Server; ReportGenerator merged line coverage ≥ 80% | 23 architecture tests | image build and push on `main` |
| `functions.yml` | `services/functions/**`, `contracts/**` (PR), the workflow | `dotnet build -c Release` | `dotnet format --verify-no-changes` | 186 tests; `coverlet.msbuild` line and branch ≥ 80% | NetArchTest and reflection tests | zip package on `main` |
| `insights.yml` | `services/insights/**`, `contracts/**` (PR), the workflow, `deploy-container-app.yml` | `uv sync --frozen` | `ruff check`, `ruff format --check` | 178 tests; coverage ≥ 85% with branches; response snapshots | `lint-imports` (7 contracts) | `mypy --strict` on `src` and `tests`; image build on every run |
| `web.yml` | `apps/web/**`, `contracts/**` (PR), the workflow | `pnpm build` (`tsc -b` + Vite) | ESLint (typescript-eslint strict type-checked, react-hooks), Prettier check | 183 tests; Vitest thresholds 70% global, 95% lines on `features/*/domain`, 90% on `shared/{format,config,http}` | `eslint-plugin-boundaries` | `tsc --noEmit` typecheck; `pnpm install --frozen-lockfile`; image build on every run |
| `infra.yml` | `infra/**`, the workflow | `az bicep build`, `az bicep build-params` for both power states | `az bicep lint` with every rule at error in `bicepconfig.json`; ShellCheck on `infra/scripts` | | | Logic App JSON check; Azure DevOps YAML parses; `az deployment group validate` and `what-if` with OIDC, written to the job summary |
| `docs.yml` | `docs/**`, the workflow | `mkdocs build --strict` (broken links and anchors fail) | | | | |
| `platform.yml` | `docker-compose.yml`, `.env.example`, `local/**`, `scripts/**`, `**/*.md`, `.github/**` | `docker compose config` (default and `functions` profile) | Markdown link check over every `.md` file, ShellCheck on `scripts/` and the web container entrypoint, actionlint | `promtool test rules` | | `promtool check config`, `amtool check-config`, Service Bus emulator JSON |

## After merge

Deploy jobs run only on pushes to `main` and target the GitHub environment `production`.

| Stage | Gate |
|---|---|
| Image publish (API, Insights, Web) | pushed to `ghcr.io/marcelo-roman/incident-ops-api`, `-insights`, `-web`, tagged with the commit SHA and `latest` |
| Production approval | `production` environment required reviewer + [release readiness](release-readiness-checklist.md) |
| Azure login | OIDC federated credential for `repo:marcelo-roman@195764956/incident-ops@1401969545:environment:production`; no stored client secret |
| Container Apps | local reusable `./.github/workflows/deploy-container-app.yml`: new revision, readiness wait, smoke test on the health URL, rollback by copying the previous revision; the API skips the readiness wait and smoke test while the environment is powered off |
| Functions, Web | `Azure/functions-action@v1` to `func-incident-ops`; `Azure/static-web-apps-deploy@v1` |
| Infra | `what-if` in the job summary before the approval; deploy on `main` |
| Post-deploy | revision promotion criteria per [SLO](../operations/slo.md) |

## Not automated yet

These are review-time expectations today, not CI gates:

- secret scanning;
- dependency vulnerability audit and container image scanning;
- Dockerfile linting (every Dockerfile runs as non-root and declares a health check, checked in review);
- OpenAPI document diff and bundle size budget;
- Playwright smoke test of the console (`pnpm test:e2e`, run on demand);
- a Functions host start against the Service Bus emulator.

## GitHub Actions and Azure DevOps equivalents

| Concern | GitHub Actions (live) | Azure DevOps (`infra/azure-devops/` sample) |
|---|---|---|
| Path filtering | `on.pull_request.paths`, `on.push.paths` | `trigger.paths.include`, `pr.paths.include` |
| PR validation | `on: pull_request` + review of the checks that ran | `pr:` trigger + build validation branch policy per path |
| Azure auth | OIDC via `azure/login@v2` | workload identity federation service connection `sc-incident-ops` |
| Approval | environment `production`, required reviewers | environment `incident-ops-prod`, approvals and branch control |
| Reuse | local reusable workflow `deploy-container-app.yml` | step template `templates/deploy-container-app.yml` |
| Secrets | repository and environment secrets | variable group `vg-incident-ops` |
| Run summary | job summary (`what-if`, coverage) | published test results, pipeline artifacts, summary tab |

## Changing a gate

Lowering a threshold needs an ADR-light: a PR to this file and to the module's configuration with the reason and an expiry date, approved by the code owner. Raising one is a normal PR.
