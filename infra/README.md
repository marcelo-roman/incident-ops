# Infrastructure

Infrastructure as code and delivery for Incident Ops: Bicep for every Azure resource in `rg-incident-ops`, the scripts for one-time setup and DNS, and Azure DevOps pipelines kept as samples. The GitHub Actions workflows that validate, preview and deploy it, and the reusable workflow that ships container app revisions with smoke test and rollback, live in [`.github/workflows/`](../.github/workflows).

## Contents

1. [Architecture](#architecture)
2. [Resources and estimated cost](#resources-and-estimated-cost)
3. [Folder layout](#folder-layout)
4. [Naming and tags](#naming-and-tags)
5. [Runtime configuration](#runtime-configuration)
6. [Security model](#security-model)
7. [Delivery with GitHub Actions](#delivery-with-github-actions)
8. [Azure DevOps samples](#azure-devops-samples)
9. [First deployment](#first-deployment)
10. [Custom domains and DNS](#custom-domains-and-dns)
11. [Alerting](#alerting)
12. [Cost controls](#cost-controls)
13. [Teardown](#teardown)
14. [Local validation](#local-validation)
15. [Design decisions](#design-decisions)
16. [How this was built](#how-this-was-built)
17. [License](#license)

## Architecture

```mermaid
flowchart LR
    user([Operator browser])
    cf[[Cloudflare DNS<br/>DNS-only records]]
    gh[[GitHub Actions<br/>OIDC to Entra ID]]
    ghcr[(GHCR public images)]

    subgraph rg[rg-incident-ops · eastus2]
        swa[Static Web App Free<br/>apps/web]
        subgraph cae[Container Apps environment · Consumption]
            api[ca-incident-ops-api<br/>.NET 8]
            ins[ca-incident-ops-insights<br/>Python]
        end
        sigr[SignalR Service Free_F1<br/>Default mode]
        sql[(Azure SQL serverless<br/>free offer)]
        subgraph sb[Service Bus Standard]
            topic{{topic incident-events}}
            subS[[sub sla-scheduler]]
            subN[[sub notifier]]
            q[[queue sla-checks]]
        end
        func[Function App Flex FC1<br/>.NET 8 isolated]
        st[(Storage<br/>host + packages)]
        logic[Logic App Consumption<br/>notify-oncall]
        oai[Azure OpenAI S0<br/>rca-drafts deployment]
        law[(Log Analytics)]
        appi[Application Insights]
        ag[Action Group]
        alerts[Availability test<br/>metric and log alerts]
    end

    user -->|incidents.*| cf
    cf --> swa
    cf --> api
    cf --> ins
    swa -. SPA calls .-> api
    swa -. negotiate / WebSocket .-> sigr
    api -->|Entra token| sql
    api -->|Data Sender| topic
    api -->|App Server| sigr
    topic --> subS
    topic --> subN
    subS -->|Data Receiver| func
    subN -->|Data Receiver| func
    func -->|Data Sender, scheduled at ackDueAt| q
    q -->|Data Receiver| func
    func -->|POST /escalate, X-Api-Key| api
    func -->|HTTP trigger SAS| logic
    logic -->|webhook| chat([Slack-compatible channel])
    func -->|Blob Data Owner| st
    ins -->|GET /api/incidents/export| api
    ins -->|OpenAI User| oai
    api & ins & func --> appi
    appi --> law
    cae --> law
    alerts --> ag
    ag -->|common alert schema, ?code=key| api
    gh -->|bicep deploy| rg
    gh -->|az containerapp update| cae
    ghcr --> cae
```

Event flow, as defined in the shared contract:

1. The API persists an incident in Azure SQL, pushes `IncidentChanged` through SignalR and publishes a CloudEvent to `incident-events` with application properties `eventType` and `severity`.
2. `sla-scheduler` (`eventType IN ('incident.triggered','incident.escalated')`) feeds `ScheduleSlaCheck`, which schedules a message on `sla-checks` at `ackDueAt`.
3. `CheckAcknowledgementSla` receives it; if the incident is still `Triggered` at the same level it calls `POST /api/incidents/{id}/escalate` and the cycle repeats up to level 3.
4. `notifier` (same filter `AND severity IN ('Sev1','Sev2')`) feeds `NotifyOnCall`, which posts to the Logic App HTTP trigger; the workflow forwards a message to the configured webhook.
5. Azure Monitor alerts reach the same API through the Action Group webhook and become incidents.

## Resources and estimated cost

Estimates are monthly, USD, `eastus2` list prices, for portfolio-level traffic (a few hundred requests per day, the API and insights scaled to zero most of the time). They are planning figures, not quotes; the budget alert is the real guardrail.

| Resource | Name | SKU / plan | Estimate |
|---|---|---|---|
| Log Analytics workspace | `log-incident-ops` | PerGB2018, 30 days, 0.25 GB/day cap | 0–6 |
| Application Insights | `appi-incident-ops` | workspace-based | included above |
| Container Apps environment | `cae-incident-ops` | Consumption workload profile | 0 |
| Container app | `ca-incident-ops-api` | 0.5 vCPU / 1 GiB, 0–2 replicas | 0–3 (monthly free grant) |
| Container app | `ca-incident-ops-insights` | 0.5 vCPU / 1 GiB, 0–2 replicas | 0–2 |
| Managed certificates | 2 | free | 0 |
| Azure SQL logical server | `sql-incident-ops-<suffix>` | Entra-only auth | 0 |
| Azure SQL database | `sqldb-incident-ops` | GP_S_Gen5_2 serverless, free offer, auto-pause on exhaustion | 0 |
| SignalR Service | `sigr-incident-ops-<suffix>` | Free_F1 (20 connections, 20k messages/day) | 0 |
| Service Bus namespace | `sbns-incident-ops-<suffix>` | Standard (topics need Standard) | ~10 |
| Function App plan | `asp-incident-ops-functions` | Flex Consumption FC1, 2048 MB instances | 0–1 (monthly free grant) |
| Function App | `func-incident-ops` | .NET 8 isolated | included above |
| Storage account | `stincidentops<suffix>` | Standard_LRS, shared key disabled | <1 |
| Logic App | `logic-incident-ops-notify` | Consumption | <1 |
| Azure OpenAI | `oai-incident-ops-<suffix>` | S0, `gpt-5.4-mini` GlobalStandard, 10k TPM | <1 (pay per token) |
| Static Web App | `stapp-incident-ops` | Free | 0 |
| Availability test | `webtest-incident-ops-api-ready` | standard test, 3 locations every 15 min | ~4 |
| Metric alerts | 4 rules | per monitored time series | <1 |
| Log alert | 1 rule | 15-minute frequency | <1 |
| Action Group | `ag-incident-ops` | webhook + optional email | 0 |
| Budget | `budget-incident-ops` | 25 USD monthly | 0 |
| **Total** | | | **~15–25** |

## Folder layout

```
infra/
  bicep/
    main.bicep                    resource group scope, wires every module
    main.bicepparam               parameters; deploy-time values come from environment variables
    naming.bicep                  exported naming function, entity names and shared types
    modules/
      monitoring.bicep            Log Analytics + workspace-based Application Insights
      sql.bicep                   Entra-only logical server + serverless free-offer database
      signalr.bicep               Free_F1, Default service mode, local auth disabled
      servicebus.bicep            topic, filtered subscriptions, queue, dead-lettering
      containerapps-environment.bicep
      containerapp.bicep          one app: identity, ingress, probes, scale, secrets, custom domain
      functions.bicep             Flex Consumption plan, identity-based storage, app settings
      logicapp.bicep              Consumption workflow loaded from workflows/notify-oncall.json
      openai.bicep                account + model deployment
      staticwebapp.bicep          Free SKU + optional custom domain
      roleassignments.bicep       entity-scoped RBAC for the workload identities
      alerting.bicep              availability test, alert rules, action group
      budget.bicep                monthly consumption budget
    workflows/notify-oncall.json  Logic App workflow definition
  bicepconfig.json                linter rules, all at error level
  azure-devops/                   Azure DevOps samples: one pipeline per deployable
    api.yml, functions.yml, insights.yml, web.yml, infra.yml
    templates/                    bicep-validate, bicep-what-if, deploy-container-app
  scripts/
    github-oidc-bootstrap.sh      one-time Entra app, federated credentials, scoped roles
    cloudflare-dns.sh             idempotent CNAME/TXT upsert from deployment outputs
    resolve-image.sh              keeps the running image when infrastructure redeploys
    sql-grant-api-identity.sh     creates the API database user from its managed identity
    ado-bootstrap.md              Azure DevOps one-time setup
.github/workflows/
  infra.yml                       Bicep build + lint, ShellCheck, ADO YAML parse, validate + what-if, deploy
  deploy-container-app.yml        reusable: update revision, smoke test, rollback
```

## Naming and tags

Names follow the Cloud Adoption Framework abbreviations (`log`, `appi`, `cae`, `ca`, `sql`, `sqldb`, `sigr`, `sbns`, `asp`, `func`, `st`, `logic`, `oai`, `stapp`). `naming.bicep` exports one function, `resourceNames(workload, suffix)`; globally unique names carry a six-character suffix from `uniqueString(resourceGroup().id)`, so names are stable across redeploys. Names fixed by the shared contract (`ca-incident-ops-api`, `ca-incident-ops-insights`, `func-incident-ops`, Service Bus entities) are not suffixed.

Every resource carries `project`, `owner`, `costCenter`, `environment` and `managedBy` tags.

## Runtime configuration

The template injects configuration as environment variables; the application modules read these names, fixed by the [contract](../contracts/contracts.md#runtime-configuration).

| Consumer | Variable | Value |
|---|---|---|
| API | `ConnectionStrings__IncidentOps` | `Server=tcp:<server>,1433;Database=sqldb-incident-ops;Authentication=Active Directory Managed Identity;Encrypt=True;...` |
| API | `ServiceBus__FullyQualifiedNamespace`, `ServiceBus__TopicName` | namespace host, `incident-events` |
| API | `Azure__SignalR__ConnectionString` | `Endpoint=https://<signalr>;AuthType=azure.msi;Version=1.0;` |
| API | `Security__EscalationApiKey` | secret reference `escalation-api-key` |
| API | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1` | custom web origin, Static Web App default origin |
| API, insights | `APPLICATIONINSIGHTS_CONNECTION_STRING`, `OTEL_SERVICE_NAME` | shared component, `incident-ops-api` / `incident-ops-insights` |
| Insights | `INCIDENTS_API_BASE_URL` | API base URL |
| Insights | `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_DEPLOYMENT` | account endpoint, `rca-drafts` |
| Insights | `CORS_ALLOWED_ORIGINS` | comma-separated web origins |
| Functions | `AzureWebJobsStorage__accountName` | identity-based host storage |
| Functions | `ServiceBusConnection__fullyQualifiedNamespace` | identity-based trigger connection |
| Functions | `IncidentEventsTopic`, `SlaSchedulerSubscription`, `NotifierSubscription`, `SlaChecksQueue` | entity names for `%binding%` expressions |
| Functions | `IncidentsApi__BaseUrl`, `IncidentsApi__ApiKey` | API base URL, escalation key |
| Functions | `Notifications__LogicAppUrl` | Logic App trigger callback URL (`listCallbackUrl`) |
| Functions | `Web__BaseUrl` | console base URL for incident links |

API base URLs switch from the `*.azurecontainerapps.io` host to the custom host when `customDomainBinding` is `SniEnabled`.

## Security model

### Workload identities

Each compute resource has a system-assigned managed identity. Role assignments are scoped to the narrowest resource the identity touches, not to the namespace or resource group.

| Identity | Role | Scope | Why |
|---|---|---|---|
| `ca-incident-ops-api` | Azure Service Bus Data Sender | topic `incident-events` | publish domain events |
| `ca-incident-ops-api` | SignalR App Server | SignalR Service | serve the hub with Entra auth |
| `ca-incident-ops-api` | `db_datareader`, `db_datawriter`, `db_ddladmin` (database roles) | `sqldb-incident-ops` | data access and EF Core migrations at startup |
| `func-incident-ops` | Azure Service Bus Data Receiver | subscriptions `sla-scheduler`, `notifier`; queue `sla-checks` | triggers |
| `func-incident-ops` | Azure Service Bus Data Sender | queue `sla-checks` | schedule SLA checks |
| `func-incident-ops` | Storage Blob Data Owner | function storage account | host leases and Flex deployment package |
| `ca-incident-ops-insights` | Cognitive Services OpenAI User | Azure OpenAI account | chat completions only |

Local authentication is disabled where the service allows it: Service Bus (`disableLocalAuth`), SignalR (`disableLocalAuth`), Azure OpenAI (`disableLocalAuth`), storage (`allowSharedKeyAccess: false`) and Azure SQL (`azureADOnlyAuthentication`). No connection string with a key exists in the template.

### Remaining shared secrets

| Secret | Where it lives | Reason |
|---|---|---|
| `ESCALATION_API_KEY` | GitHub secret → Container Apps secret, Function app setting, Action Group webhook `code` | the contract authenticates machine callers of `/escalate` and `/api/alerts/*` with an API key; Azure Monitor webhooks cannot send headers |
| `NOTIFICATION_WEBHOOK_URL` | GitHub secret → Logic App `securestring` parameter | third-party webhook |
| Logic App callback URL | Function app setting, resolved at deploy time with `listCallbackUrl()` | Consumption HTTP triggers authenticate with SAS |
| `SWA_DEPLOYMENT_TOKEN` | GitHub repository secret | Static Web Apps deploy action |

Key Vault references are the next step for the Function app settings; they were left out to keep the resource count and the cost line at zero for a single-key setup.

### Deployment principal

`scripts/github-oidc-bootstrap.sh` creates `sp-incident-ops-github` with federated credentials only (no client secret):

| Credential | Subject | Used by |
|---|---|---|
| `incident-ops-production` | `repo:marcelo-roman/incident-ops:environment:production` | every deploy job (`api.yml`, `functions.yml`, `insights.yml`, `infra.yml`, and the reusable `deploy-container-app.yml`) |
| `incident-ops-main` | `repo:marcelo-roman/incident-ops:ref:refs/heads/main` | `infra.yml` what-if on `main` before the approval gate |

One repository holds every module, so two subjects cover all workflows. There is deliberately no `pull_request` subject: a pull request, even one that edits a workflow, cannot obtain an Azure token. `GITHUB_OWNER` and `GITHUB_REPOSITORY_NAME` override the defaults.

Roles, both scoped to `rg-incident-ops`:

- `Contributor`.
- `Role Based Access Control Administrator` with an ABAC condition: it can only create or delete assignments of the five workload roles above, and only for principals of type `ServicePrincipal`. The pipeline cannot grant itself `Owner` or hand roles to users.

The pull request credential shares the principal; protect `main`, require reviews and keep fork pull requests without secrets (the GitHub default). A stricter variant is a second principal with a custom role limited to `deployments/validate/action` and `deployments/whatIf/action` plus `*/read`.

### Network

All endpoints are public with TLS 1.2 minimum. Azure SQL allows Azure services (`0.0.0.0` rule) because Consumption Container Apps without a VNet have no fixed egress IP. Private endpoints and a VNet-integrated environment are the production hardening path; they add roughly 7–8 USD per private endpoint per month and were left out of a 25 USD budget.

## Delivery with GitHub Actions

GitHub Actions is the live delivery path. Authentication uses `azure/login@v2` with OpenID Connect and the repository variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`. Every workflow is path-filtered, so a change under `infra/` runs only `infra.yml` (plus `platform.yml` for Markdown links when a `.md` file changes).

### Workflows

| Workflow | Trigger | Steps |
|---|---|---|
| [`infra.yml`](../.github/workflows/infra.yml) → `bicep`, `scripts` | pull requests to `main` and pushes to `main` that change `infra/**` or the workflow | `az bicep build`, `az bicep build-params`, `az bicep lint` (all rules at error), Logic App JSON check, ShellCheck on `scripts/*.sh`, Azure Pipelines YAML parse for `azure-devops/` |
| `infra.yml` → `plan` | pushes to `main` only, after `bicep`, when `DEPLOY_ENABLED` is `true` | OIDC login, build and lint, resolve running images, `az deployment group validate`, `az deployment group what-if` written to the job summary and uploaded as the `what-if` artifact |
| `infra.yml` → `deploy` | pushes to `main` only, when `DEPLOY_ENABLED` is `true` | waits for the `production` environment approval, `az deployment group create`, Cloudflare DNS upsert when `CLOUDFLARE_API_TOKEN` is set, outputs in the job summary |
| [`deploy-container-app.yml`](../.github/workflows/deploy-container-app.yml) | `workflow_call` from `api.yml` and `insights.yml` | see below |
| [`platform.yml`](../.github/workflows/platform.yml) → `workflows` | changes under `.github/**` | actionlint over every workflow |

Steps run with `defaults.run.working-directory: infra`. Every Azure job (infra plan and deploy, and the deploy jobs of `api.yml`, `functions.yml`, `insights.yml` and `web.yml`) also requires the repository variable `DEPLOY_ENABLED=true`, so until the first deployment exists those jobs report as skipped rather than failed. The `production` and `github-pages` environments accept deployments from `main` only. The reviewer sees the what-if summary of the same run before approving the `deploy` job.

### Gates

- `production` environment with a required reviewer and deployment branch policy "protected branches only"; the deploy job also checks `github.ref == 'refs/heads/main'`.
- Ruleset on `main`: pull request required, review from code owners ([`.github/CODEOWNERS`](../.github/CODEOWNERS)), and the `infra` checks (`Bicep build and lint`, `ShellCheck and Azure DevOps samples`, `Validate and what-if`) green on pull requests that change `infra/**`.
- `bicepconfig.json` raises every core linter rule to `error`, including `use-recent-api-versions`, `outputs-should-not-contain-secrets`, `secure-secrets-in-params` and `what-if-short-circuiting`, so a warning fails the build.

### Container app releases and rollback

`deploy-container-app.yml` is a local reusable workflow. `api.yml` and `insights.yml` call it after they push an image to GHCR:

```yaml
jobs:
  deploy:
    needs: image
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    permissions:
      contents: read
      id-token: write
    uses: ./.github/workflows/deploy-container-app.yml
    with:
      container-app-name: ca-incident-ops-api
      resource-group: rg-incident-ops
      image: ghcr.io/marcelo-roman/incident-ops-api:${{ github.sha }}
      health-url: https://incidents-api.marceloroman.com.br/health/ready
    secrets: inherit
```

The caller grants `id-token: write`. The reusable workflow runs in the `production` environment, so the environment's approval rule and the `environment:production` federated credential apply.

1. Records `latestReadyRevisionName` and its image.
2. `az containerapp update --image` with revision suffix `gh<run id>-<attempt>`, so every run creates a revision even when a tag is reused.
3. Polls until the new revision is `latestReadyRevisionName`; fails fast if its provisioning state is `Failed`.
4. Polls `health-url` until it answers 2xx.
5. On any failure, `az containerapp revision copy --from-revision <previous>` restores the previous revision's template (image, environment, probes) as a new active revision; in single revision mode it takes all traffic.
6. Writes a job summary with previous and new revisions and the outcome of each step.

Infrastructure redeploys do not undo application releases: `scripts/resolve-image.sh` reads the image currently running and passes it to Bicep, falling back to `:latest` only before the first release.

## Azure DevOps samples

[`azure-devops/`](azure-devops) holds one pipeline per deployable (`api.yml`, `functions.yml`, `insights.yml`, `web.yml`, `infra.yml`) and the step templates in `azure-devops/templates/`. They are samples: delivery runs on GitHub Actions, and the pipelines are not wired to any Azure DevOps project. Each one uses `trigger.paths.include` and `pr.paths.include` for its module folder, checks out the repository once with `checkout: self`, sets `workingDirectory` to its module and references templates by relative path. [`scripts/ado-bootstrap.md`](scripts/ado-bootstrap.md) lists the one-time setup: project, service connection `sc-incident-ops` with workload identity federation, registry connection `ghcr-marcelo-roman`, variable group `vg-incident-ops` and environment `incident-ops-prod` with approval and branch control checks.

| Stage of `infra.yml` | Condition | Steps |
|---|---|---|
| Validate | always | `templates/bicep-validate.yml`: build, lint, build-params, `az deployment group validate` |
| WhatIf | after Validate | `templates/bicep-what-if.yml`: what-if published as artifact and run summary |
| Deploy | `main`, not a pull request | deployment job on `incident-ops-prod`; approvals and branch control are configured on the environment in Azure DevOps |

`templates/deploy-container-app.yml` mirrors the GitHub reusable workflow (parameters `containerAppName`, `image`, `resourceGroup`, `azureSubscription`, `healthUrl`) and is used by `api.yml` and `insights.yml`.

## First deployment

Nothing here runs automatically; these are the manual steps, in order.

1. Sign in with an account that can create app registrations and assign roles, then run the bootstrap. It is idempotent.

   ```bash
   AZURE_SUBSCRIPTION_ID=<subscription id> infra/scripts/github-oidc-bootstrap.sh
   ```

   It creates `rg-incident-ops`, `sp-incident-ops-github`, the three federated credentials for `marcelo-roman/incident-ops`, the two scoped role assignments and the Entra group `sg-incident-ops-sql-admins` (with you as a member), then prints the `gh` commands for repository variables, secrets and the `production` environment.
2. Run the printed `gh` commands. `ESCALATION_API_KEY` must be at least 32 characters.
3. In the repository settings: a ruleset on `main` (pull request, review from code owners, block force pushes and deletion), GitHub Pages with source "GitHub Actions", and Actions permission to create the `github-pages` environment.
4. Publish at least one image of `incident-ops-api` and `incident-ops-insights` to GHCR and make both packages public (the first `main` run of `api.yml` and `insights.yml` pushes them; their deploy jobs fail until the container apps exist).
5. Run `infra.yml` on `main` (push or `workflow_dispatch`) and approve the `deploy` job.
6. Grant the API identity access to the database. Run as a member of `sg-incident-ops-sql-admins`, with go-sqlcmd installed and `az login` done:

   ```bash
   infra/scripts/sql-grant-api-identity.sh
   ```

   Creating a contained user `FROM EXTERNAL PROVIDER` resolves the identity through Microsoft Graph with the caller's token, which is why this runs once as a person rather than in the pipeline. Automating it requires the `Directory Readers` role for a SQL server identity.
7. Set the `SWA_DEPLOYMENT_TOKEN` repository secret with the command the bootstrap printed, then re-run `api.yml`, `insights.yml` and `web.yml` on `main`.
8. Follow [Custom domains and DNS](#custom-domains-and-dns).

## Custom domains and DNS

DNS for `marceloroman.com.br` lives in Cloudflare. Records must be DNS-only (grey cloud): Container Apps managed certificates and Static Web Apps validate the CNAME directly.

| Host | Record | Target |
|---|---|---|
| `incidents.marceloroman.com.br` | CNAME | Static Web App default host name (validated by CNAME delegation) |
| `incidents-api.marceloroman.com.br` | CNAME | `ca-incident-ops-api` default FQDN |
| `asuid.incidents-api.marceloroman.com.br` | TXT | `containerAppsVerificationId` output |
| `incidents-insights.marceloroman.com.br` | CNAME | `ca-incident-ops-insights` default FQDN |
| `asuid.incidents-insights.marceloroman.com.br` | TXT | `containerAppsVerificationId` output |
| `incidents-docs.marceloroman.com.br` | CNAME | `marcelo-roman.github.io` (GitHub Pages of `marcelo-roman/incident-ops`) |

The template outputs these records as `dnsRecords`, and `scripts/cloudflare-dns.sh` creates or updates them idempotently (`unchanged`, `created`, `updated`), forcing `proxied: false`. The `deploy` job runs it when `CLOUDFLARE_API_TOKEN` is set; the token needs `Zone.DNS:Edit` on the zone. Manually:

```bash
az deployment group show --resource-group rg-incident-ops --name incident-ops \
  --query properties.outputs.dnsRecords.value > dns-records.json
CLOUDFLARE_API_TOKEN=<token> scripts/cloudflare-dns.sh dns-records.json
```

A managed certificate can only be issued after the host name is on the app, and the binding can only reference a certificate that exists, so binding takes three deployments driven by the `CUSTOM_DOMAIN_BINDING` repository variable:

| Value | Effect |
|---|---|
| `None` | no custom domains; DNS records are created from the outputs |
| `Disabled` | host names added to both apps without a certificate, managed certificates issued, Static Web App custom domain added |
| `SniEnabled` | certificates bound; API and insights base URLs switch to the custom hosts |

For GitHub Pages, set the custom domain `incidents-docs.marceloroman.com.br` in the repository's Pages settings after the CNAME exists, and update `site_url` in `docs/mkdocs.yml` to match.

## Alerting

`alerting.bicep` routes every rule to one Action Group whose webhook targets `<api>/api/alerts/azure-monitor?code=<ESCALATION_API_KEY>` with the common alert schema, so alerts become incidents through the API's alert ingestion. An optional email receiver is added when `ALERT_EMAIL` is set.

| Rule | Signal | Condition | Severity |
|---|---|---|---|
| `alert-incident-ops-api-availability` | standard availability test on `/health/ready`, 3 locations, every 15 min | 2 or more locations failing | Sev1 |
| `alert-incident-ops-api-failed-requests` | `requests/failed` for role `incident-ops-api` | more than 5 in 5 min | Sev2 |
| `alert-incident-ops-api-response-time` | `requests/duration` average for role `incident-ops-api` | above 2000 ms over 15 min | Sev3 |
| `alert-<namespace>-dead-letters` | Service Bus `DeadletteredMessages`, split by entity | above 0 over 15 min | Sev2 |
| `alert-func-incident-ops-failures` | log query on `requests` for role `func-incident-ops` | any failed execution in 15 min, split by function | Sev3 |

Every rule sends `customProperties.service = platform` (metric alerts through the action's `webHookProperties`, the log alert through `actions.customProperties`), which maps to the seeded `platform` service. Rules auto-resolve, and the resolved notification moves the incident to `Mitigated`.

Choices: availability uses the location-based criterion rather than `availability < 100%` on a single location so one cold start or one probe region does not page Sev1. Metric alerts cannot compute percentiles, so response time uses the average; a p95 rule needs a log alert on `requests | summarize percentile(duration, 95)`, at roughly 1.5 USD per month more.

## Cost controls

- Monthly budget of 25 USD on the resource group with notifications at 50% and 100% actual and 100% forecasted, to `BUDGET_EMAIL`.
- Azure SQL free offer with `freeLimitExhaustionBehavior: AutoPause`: once the monthly free vCore seconds or storage are used up, the database pauses instead of billing.
- Container apps and the Function app scale to zero; their free monthly grants cover expected traffic.
- Log Analytics daily cap of 0.25 GB and 30-day retention.
- SignalR and Static Web Apps on Free SKUs.
- Availability test kept to 3 locations every 15 minutes.
- Azure OpenAI is pay-per-token with a 10k TPM ceiling.

## Teardown

```bash
az group delete --name rg-incident-ops --yes
az cognitiveservices account purge --location eastus2 --resource-group rg-incident-ops --name <oai-incident-ops-suffix>
az ad app delete --id "$(az ad app list --display-name sp-incident-ops-github --query '[0].appId' --output tsv)"
az ad group delete --group sg-incident-ops-sql-admins
```

Azure OpenAI accounts are soft-deleted for 48 hours; the purge frees the custom subdomain. Delete the Cloudflare records (filter by comment `incident-ops`) and the GitHub `production` environments afterwards.

## Local validation

Requires Azure CLI with Bicep (`az bicep install`), ShellCheck and actionlint. Run from `infra/`, except actionlint, which runs from the repository root:

```bash
az bicep build --file bicep/main.bicep --stdout > /dev/null
az bicep lint --file bicep/main.bicep
ESCALATION_API_KEY=$(openssl rand -hex 32) az bicep build-params --file bicep/main.bicepparam --stdout > /dev/null
shellcheck --severity=style scripts/*.sh
(cd .. && actionlint)
```

## Design decisions

- **Resource group scope.** The contract fixes one resource group in one region, and the deployment principal is scoped to that group. A subscription-scope template would need subscription-level rights just to create the group; `scripts/github-oidc-bootstrap.sh` creates it once instead, and the budget is a resource-group budget. `az deployment group validate` and `what-if` then work with the same least-privilege principal.
- **Model.** The template defaults to `gpt-5.4-mini` `2026-03-17` (GA, GlobalStandard in `eastus2`) under a model-neutral deployment name, `rca-drafts`, so the insights service never changes when the model does. `openAiModelName` and `openAiModelVersion` override it, and `versionUpgradeOption: OnceCurrentVersionExpired` avoids silent upgrades. Models in the Deprecated lifecycle stage, such as `gpt-4o-mini` `2024-07-18`, cannot be deployed by subscriptions that never used them.
- **Role assignment names** derive from the scope, the role and the identity's resource name, not from the principal id, so what-if can predict them. Recreating an app gives it a new principal; delete its old assignment first.
- **Single revision mode with copy-based rollback.** Traffic splitting in multiple revision mode conflicts with Bicep redeploys, which reset traffic to the latest revision. Copying the previous revision keeps the running template and Bicep's resolved image in agreement.
- **Explicit `dependsOn`** where modules receive names instead of outputs: names known at compile time keep what-if precise (`what-if-short-circuiting`).
- **No diagnostic settings resources.** Container Apps and Functions already send logs to the workspace. The current `Microsoft.Insights/diagnosticSettings` API version is a 2021 preview, which the strict `use-recent-api-versions` rule rejects.
- **Logic App webhook format.** The workflow posts `{ "text": ... }`, the Slack incoming-webhook format; Teams Workflows webhooks need an Adaptive Card body instead. Email needs an Office 365 API connection with interactive consent, so it is not part of the template.

## How this was built

The architecture, resource choices and security model are the author's design, and changes go through the author's review. Implementation was AI-assisted with Claude Code working from the shared [contract](../contracts/contracts.md), and every change is checked with the commands in [Local validation](#local-validation) and by the CI workflows.

## License

[MIT](../LICENSE)
