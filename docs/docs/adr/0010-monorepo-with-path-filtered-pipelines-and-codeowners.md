---
status: accepted
date: 2026-10-02
deciders: Marcelo Roman
---

# Monorepo with path-filtered pipelines and CODEOWNERS

## Context and problem statement

Incident Ops has four deployable services in three stacks (.NET, Python, TypeScript), Bicep infrastructure, a documentation site, a local stack and a contract every module implements. A contract change touches several modules at once and must be reviewed as one change. Each module still has to build, test and deploy on its own. How is the source organized and how does CI decide what to run?

## Decision drivers

- A contract change and its implementations are reviewed and merged together.
- A change to one module does not run or deploy the others.
- One place for the delivery pipeline, the OIDC trust and the review rules.
- No build tool that only one stack understands.

## Considered options

1. One repository per module, with cross-repository reusable workflows
2. One repository, one GitHub Actions workflow per module filtered by path, CODEOWNERS per top-level path
3. One repository with a build orchestrator (Nx, Bazel) computing affected projects

## Decision outcome

Chosen option: **one repository with path-filtered workflows and CODEOWNERS**.

- Layout: `services/api`, `services/functions`, `services/insights`, `apps/web`, `infra`, `docs`, `contracts`, `local`, `.github`.
- `.github/workflows/` has `api.yml`, `functions.yml`, `insights.yml`, `web.yml`, `infra.yml`, `docs.yml` and `platform.yml`. Each filters `on.pull_request.paths` and `on.push.paths` to its folder and its own file, and runs with `defaults.run.working-directory` set to the module. Pull requests that change `contracts/**` run all four application workflows.
- Container app deploys call the local reusable workflow `./.github/workflows/deploy-container-app.yml`.
- One set of OIDC federated credentials for `repo:marcelo-roman/incident-ops` (`environment:production`, `pull_request`, `ref:refs/heads/main`).
- `.github/CODEOWNERS` assigns an owner per top-level path, with `/contracts/` last so its rule wins.
- Azure DevOps samples follow the same shape: one pipeline per deployable in `infra/azure-devops/` with `trigger.paths.include`.

### Consequences

- Good: a contract change is one pull request with its consumers' tests running on it.
- Good: each module keeps its own toolchain, lockfile and quality gates; path filters keep CI time proportional to the change.
- Good: one federated identity, one `production` environment, one set of repository variables.
- Bad: GitHub reports a required status check from a workflow skipped by its path filter as pending, so per-path required checks cannot be enforced by the ruleset alone ([branching](../engineering/branching-and-git-workflow.md#required-checks-per-path)).
- Bad: path filters do not see indirect dependencies; a change in `local/` does not run the API tests. The contract filter covers the one dependency that crosses modules.
- Bad: repository-wide settings (branch rules, secrets) apply to every module.

## Pros and cons of the options

### One repository per module

- Good: independent permissions and history per module.
- Bad: a contract change becomes several coordinated pull requests; reusable workflows and templates are consumed across repositories by ref; one federated credential per repository.

### Build orchestrator

- Good: exact affected-project detection and caching.
- Bad: Nx and Bazel need adapters for .NET and Python and a second build definition next to each native toolchain; the module count does not justify it.
