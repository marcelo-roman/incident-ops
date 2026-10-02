---
status: accepted
date: 2026-09-10
deciders: Marcelo Roman
---

# Bicep over Terraform

## Context and problem statement

All resources live in one Azure subscription and one resource group (`rg-incident-ops`). Delivery runs in GitHub Actions with OIDC to Azure, and the same stages must be expressible as Azure DevOps YAML. Which infrastructure-as-code tool do we use?

## Decision drivers

- Azure-only footprint, no other cloud or SaaS providers to manage.
- No state file to secure, lock and back up.
- Day-zero support for new Azure resource types and API versions.
- Preview of changes on every pull request.

## Considered options

1. Bicep with `az deployment group what-if`
2. Terraform with the `azurerm` provider
3. ARM JSON

## Decision outcome

Chosen option: **Bicep**, one module per resource, a `main.bicep` composing them, parameters per environment in `.bicepparam` files. Pull requests run `what-if` and publish the diff to the job summary; merges deploy behind the `production` environment.

### Consequences

- Good: Azure is the state; nothing to store or lock, and no state-storage credentials in the CI system.
- Good: `az deployment group` commands run the same in GitHub Actions and Azure DevOps, so the sample pipeline `infra/azure-devops/infra.yml` mirrors the workflow one to one.
- Good: new resource types (Flex Consumption, Container Apps features) are usable on release day.
- Good: `bicep lint` and `what-if` slot into the [quality gates](../engineering/quality-gates.md).
- Bad: Cloudflare DNS records are outside Bicep's reach; they are managed by hand and listed in the infra README. If DNS automation becomes necessary, Terraform for that provider alone is acceptable.
- Bad: `what-if` produces noise on some resource properties; reviewers learn which ones to ignore, documented in the infra README.

## Pros and cons of the options

### Terraform

- Good: multi-provider (could own Cloudflare DNS); large ecosystem; `plan` is precise.
- Bad: remote state, locking and secrets in state are an extra responsibility; provider lag for new Azure features.

### ARM JSON

- Good: native.
- Bad: verbose; Bicep compiles to it anyway.
