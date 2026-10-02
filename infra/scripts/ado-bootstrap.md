# Azure DevOps setup (sample path)

GitHub Actions is the live delivery path for Incident Ops. The pipelines in [`infra/azure-devops/`](../azure-devops/) are samples: they implement the same flow for Azure DevOps and are not wired to any project. This page describes the one-time setup that makes them runnable. Nothing here is required for the GitHub path.

| Pipeline | Builds and deploys | Path filter (`trigger.paths.include`) |
|---|---|---|
| [`api.yml`](../azure-devops/api.yml) | `services/api` → `ca-incident-ops-api` | `services/api`, the pipeline file, the container app template |
| [`functions.yml`](../azure-devops/functions.yml) | `services/functions` → `func-incident-ops` | `services/functions`, the pipeline file |
| [`insights.yml`](../azure-devops/insights.yml) | `services/insights` → `ca-incident-ops-insights` | `services/insights`, the pipeline file, the container app template |
| [`web.yml`](../azure-devops/web.yml) | `apps/web` → Static Web App | `apps/web`, the pipeline file |
| [`infra.yml`](../azure-devops/infra.yml) | `infra/bicep` → `rg-incident-ops` | `infra/bicep`, `infra/bicepconfig.json`, `infra/scripts`, the pipeline file, the Bicep templates |

Pull request triggers use the same filters plus `contracts` for the four application pipelines. Every pipeline checks out the repository once (`checkout: self`) and runs its steps with `workingDirectory` set to its module. Step templates live in [`infra/azure-devops/templates/`](../azure-devops/templates/) and are referenced by relative path, so no pipeline needs a second repository resource.

## Contents

1. [Project](#project)
2. [Azure service connection](#azure-service-connection)
3. [Container registry service connection](#container-registry-service-connection)
4. [Variable group](#variable-group)
5. [Environment and approvals](#environment-and-approvals)
6. [Pipelines](#pipelines)
7. [Container app template](#container-app-template)

## Project

```bash
az extension add --name azure-devops
az devops configure --defaults organization=https://dev.azure.com/<organization>
az devops project create --name incident-ops --visibility private
```

## Azure service connection

Name: `sc-incident-ops`. Type: Azure Resource Manager, workload identity federation (no client secret).

1. Project settings → Service connections → New → Azure Resource Manager → **App registration (automatic)** with **Workload identity federation**, scope **Resource group** `rg-incident-ops`. Name it `sc-incident-ops`.
2. The wizard grants `Contributor` on the resource group. Bicep also writes role assignments, so add `Role Based Access Control Administrator` on the same scope, restricted with the same ABAC condition `scripts/github-oidc-bootstrap.sh` uses (only the five workload roles, only to service principals). Print the condition with `scripts/github-oidc-bootstrap.sh --print-rbac-condition`, then:

```bash
az role assignment create \
  --assignee-object-id <service-connection-principal-object-id> \
  --assignee-principal-type ServicePrincipal \
  --role "Role Based Access Control Administrator" \
  --scope "$(az group show --name rg-incident-ops --query id --output tsv)" \
  --condition "<condition>" \
  --condition-version "2.0"
```

To reuse the GitHub principal instead, create the connection manually (**App registration or managed identity (manual)**), enter the `sp-incident-ops-github` client id and add the federated credential Azure DevOps displays (issuer `https://vstoken.dev.azure.com/<organization-id>`, subject `sc://<organization>/incident-ops/sc-incident-ops`) to that app registration.

## Container registry service connection

Name: `ghcr-marcelo-roman`. Type: Docker Registry, registry `https://ghcr.io`, with a GitHub personal access token that has `write:packages`. `api.yml` and `insights.yml` push their images through it.

## Variable group

Name: `vg-incident-ops`, under Pipelines → Library. The pipeline exposes non-secret variables as environment variables with the same name, which `bicep/main.bicepparam` reads through `readEnvironmentVariable`.

| Variable | Secret | Example |
|---|---|---|
| `SQL_ADMIN_GROUP_NAME` | no | `sg-incident-ops-sql-admins` |
| `SQL_ADMIN_GROUP_OBJECT_ID` | no | Entra group object id |
| `BUDGET_EMAIL` | no | budget alert recipient |
| `ALERT_EMAIL` | no | optional Azure Monitor alert recipient |
| `CUSTOM_DOMAIN_BINDING` | no | `None`, `Disabled` or `SniEnabled` |
| `ESCALATION_API_KEY` | yes | `openssl rand -hex 32` |
| `NOTIFICATION_WEBHOOK_URL` | yes | Slack-compatible incoming webhook |
| `SWA_DEPLOYMENT_TOKEN` | yes | Static Web App deployment token, read by `web.yml` |

Secret variables are mapped explicitly into the task `env` block; Azure Pipelines never exposes them implicitly.

## Environment and approvals

Name: `incident-ops-prod`, under Pipelines → Environments.

1. Approvals and checks → **Approvals**: add the approvers; the `Deploy` stage waits until one of them approves.
2. Approvals and checks → **Branch control**: allowed branches `refs/heads/main`, and enable "Verify branch protection". This enforces the main-only rule in addition to the stage conditions in the pipelines.
3. Optional: **Business hours** check.

Approvals and checks live in Azure DevOps, not in YAML, so a pull request cannot remove them.

## Pipelines

Create one pipeline per file: Pipelines → New pipeline → GitHub → `marcelo-roman/incident-ops` → Existing YAML → `/infra/azure-devops/<name>.yml`. Name each pipeline after its file (`api`, `functions`, `insights`, `web`, `infra`). Grant each one permission to `vg-incident-ops`, `sc-incident-ops`, `ghcr-marcelo-roman` and `incident-ops-prod` on its first run.

`infra.yml`:

| Stage | Runs on | Does |
|---|---|---|
| Validate | every run | `az bicep build`, `az bicep lint`, `az bicep build-params`, `az deployment group validate` |
| WhatIf | every run | `az deployment group what-if`, published as the `what-if` artifact and as a run summary tab |
| Deploy | `main`, not pull requests | deployment job on `incident-ops-prod` (approval + branch control), `az deployment group create` |

Application pipelines:

| Pipeline | Stages |
|---|---|
| `api.yml` | Build (restore, format check, build, tests, 80% line coverage gate) → Publish (image to GHCR) → Deploy (container app template) |
| `insights.yml` | Verify (ruff, format check, mypy, import-linter, pytest) → Publish (image to GHCR) → Deploy (container app template) |
| `functions.yml` | Build (restore, format check, build, tests with coverage, zip package) → Deploy (`AzureFunctionApp@2`, Flex Consumption) |
| `web.yml` | Build (install, lint, format check, typecheck, tests with coverage, build) → Deploy (`AzureStaticWebApp@0`) |

Publish and Deploy stages run only on `main` outside pull requests.

## Container app template

`templates/deploy-container-app.yml` mirrors the GitHub reusable workflow: it updates the image with a new revision suffix, waits for readiness, smoke tests the health URL and copies the previous revision back on failure. `api.yml` and `insights.yml` consume it from a deployment job:

```yaml
- deployment: ContainerApp
  environment: incident-ops-prod
  strategy:
    runOnce:
      deploy:
        steps:
          - template: templates/deploy-container-app.yml
            parameters:
              containerAppName: ca-incident-ops-api
              image: ghcr.io/marcelo-roman/incident-ops-api:$(Build.SourceVersion)
              resourceGroup: rg-incident-ops
              azureSubscription: sc-incident-ops
              healthUrl: https://incidents-api.marceloroman.com.br/health/ready
```
