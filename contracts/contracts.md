# Contracts

Shared contract between the Incident Ops modules. Every module implements against this document.

## Contents

1. [Modules](#modules)
2. [Domain](#domain)
3. [SLA policy](#sla-policy)
4. [On-call and escalation](#on-call-and-escalation)
5. [HTTP API](#http-api)
6. [Real-time hub](#real-time-hub)
7. [Events](#events)
8. [Alert ingestion](#alert-ingestion)
9. [Insights API](#insights-api)
10. [Azure topology](#azure-topology)
11. [Delivery](#delivery)
12. [Conventions](#conventions)

## Modules

Every module lives in the `marcelo-roman/incident-ops` repository.

| Path | Bounded context | Role | Stack |
| --- | --- | --- | --- |
| [`services/api`](../services/api) | Incident Management | system of record for incidents, SLA clock, escalation, real-time hub | .NET 8, ASP.NET Core Minimal APIs, EF Core, SignalR, Azure Service Bus |
| [`services/functions`](../services/functions) | Escalation | SLA watchdog and escalation notifications | Azure Functions (.NET 8 isolated), Service Bus, Logic Apps |
| [`services/insights`](../services/insights) | Operational Analytics | KTLO analytics and AI-assisted RCA | Python, FastAPI, Pandas, NumPy, scikit-learn, Azure OpenAI |
| [`apps/web`](../apps/web) | Operations Console | operations console | React, TypeScript, Vite, @microsoft/signalr |
| [`infra`](../infra) | | infrastructure and delivery samples | Bicep, Azure DevOps YAML samples |
| [`docs`](../docs) | | documentation site: architecture diagrams, operations model, alerting, runbooks, engineering standards, ADRs | MkDocs Material, Mermaid, GitHub Pages |
| [`contracts`](.) | | this contract | Markdown |
| [`local`](../local), [`docker-compose.yml`](../docker-compose.yml) | | local full stack (incl. Prometheus and Alertmanager) | Docker Compose |
| [`.github/workflows`](../.github/workflows) | | delivery | GitHub Actions |

GitHub owner: `marcelo-roman`. Container images: `ghcr.io/marcelo-roman/incident-ops-<module>` (`incident-ops-api`, `incident-ops-insights`, `incident-ops-web`).

## Domain

### Service

| Field | Type |
| --- | --- |
| `id` | string slug (`checkout`, `payments-gateway`, `identity`, `notifications`, `search`, `reporting`, `platform`) |
| `name` | string |
| `tier` | `Tier1` \| `Tier2` \| `Tier3` |
| `ownerTeam` | string |

### Incident

| Field | Type |
| --- | --- |
| `id` | uuid |
| `number` | int, human friendly, sequential (`INC-1042`) |
| `title` | string |
| `description` | string |
| `serviceId` | string |
| `severity` | `Sev1` \| `Sev2` \| `Sev3` \| `Sev4` |
| `status` | `Triggered` \| `Acknowledged` \| `Mitigated` \| `Resolved` |
| `assignee` | string \| null |
| `escalationLevel` | int, starts at 1 |
| `createdAt`, `acknowledgedAt`, `mitigatedAt`, `resolvedAt` | ISO-8601 UTC \| null |
| `ackDueAt`, `resolveDueAt` | ISO-8601 UTC; `ackDueAt` is the deadline of the current acknowledgement window and moves on every escalation |
| `acknowledgementBreached` | bool; set when an escalation happens or when the acknowledgement arrives after `ackDueAt`; never reset |
| `slaState` | `OnTrack` \| `AtRisk` \| `Breached` \| `Met` |
| `rootCause` | string \| null |
| `source` | `Manual` \| `Alertmanager` \| `AzureMonitor` |
| `alertFingerprint` | string \| null |

Status transitions: `Triggered → Acknowledged → Mitigated → Resolved`. `Triggered → Resolved`, `Triggered → Mitigated` (an alert resolving before anyone acknowledged) and `Acknowledged → Resolved` are allowed. Any other transition is rejected with `409`.

SLA compliance (any metric, in the API and in Insights): an incident complies when it was acknowledged, `acknowledgementBreached` is false, and it was resolved by `resolveDueAt`. Compliance never compares against the current `ackDueAt`, because escalation moves it. The compliance percentage for a window takes incidents created in the window that are either resolved or open but already unable to comply (`acknowledgementBreached` true or `slaState` `Breached`); open incidents that can still comply are not counted yet. `slaState` is the live indicator for a single incident and is computed as below.

`slaState`: once resolved, `Met` when the incident complies and `Breached` otherwise; `Breached` when an open deadline has passed (ack while `Triggered`, resolve while not `Resolved`); `AtRisk` when less than 25% of the active window remains; `OnTrack` otherwise.

### TimelineEntry

| Field | Type |
| --- | --- |
| `id` | uuid |
| `incidentId` | uuid |
| `at` | ISO-8601 UTC |
| `kind` | `Triggered` \| `Acknowledged` \| `Escalated` \| `Mitigated` \| `Resolved` \| `Note` \| `Alert` |
| `actor` | string |
| `message` | string |

## SLA policy

| Severity | Acknowledge within | Resolve within |
| --- | --- | --- |
| `Sev1` | 15 minutes | 4 hours |
| `Sev2` | 30 minutes | 8 hours |
| `Sev3` | 4 hours | 3 days |
| `Sev4` | 1 business day (24h) | 10 days |

## On-call and escalation

Weekly rotation of six engineers, primary and secondary, one week on call every six. Rotation starts Monday 10:30 America/New_York.

| Level | Target |
| --- | --- |
| 1 | primary on call |
| 2 | secondary on call |
| 3 | engineering lead |

An incident not acknowledged by `ackDueAt` escalates one level. Escalation stops at level 3.

## HTTP API

Base URL: `https://incidents-api.marceloroman.com.br`. JSON, camelCase, enums as strings, timestamps ISO-8601 UTC. Errors as RFC 7807 `application/problem+json`.

| Method | Path | Body | Result |
| --- | --- | --- | --- |
| GET | `/api/services` | | `Service[]` |
| GET | `/api/incidents?status=&severity=&serviceId=&open=true` | | `Incident[]` newest first |
| GET | `/api/incidents/{id}` | | `Incident` with `timeline: TimelineEntry[]` |
| POST | `/api/incidents` | `{ title, description, serviceId, severity }` | `201 Incident` |
| POST | `/api/incidents/{id}/acknowledge` | `{ actor }` | `Incident` |
| POST | `/api/incidents/{id}/escalate` | `{ reason }` | `Incident`; requires `X-Api-Key` |
| POST | `/api/incidents/{id}/mitigate` | `{ actor, note }` | `Incident` |
| POST | `/api/incidents/{id}/resolve` | `{ actor, rootCause }` | `Incident` |
| POST | `/api/incidents/{id}/notes` | `{ actor, message }` | `TimelineEntry` |
| GET | `/api/oncall/current` | | `{ weekStart, primary, secondary, lead }` |
| GET | `/api/metrics/summary` | | `{ openBySeverity, slaCompliance30d, mtta30dMinutes, mttr30dMinutes, breachedOpen }` |
| GET | `/api/incidents/export?from=&to=` | | flat `Incident[]` without timeline, for analytics |
| GET | `/health/live`, `/health/ready` | | health checks |

Write endpoints are rate limited per IP. The database is seeded with the seven services, the rotation and six months of historical incidents generated deterministically so analytics have data.

## Real-time hub

Path `/hubs/incidents` (Azure SignalR Service in production, in-process locally). Server to client:

| Method | Arguments |
| --- | --- |
| `IncidentChanged` | `Incident` |
| `TimelineAppended` | `TimelineEntry` |

## Events

Azure Service Bus, CloudEvents 1.0 structured JSON (`application/cloudevents+json`).

```json
{
  "specversion": "1.0",
  "id": "uuid",
  "type": "incident.triggered",
  "source": "incident-ops-api",
  "time": "2026-10-02T14:00:00Z",
  "subject": "<incident id>",
  "datacontenttype": "application/json",
  "data": { "...Incident" }
}
```

Types: `incident.triggered`, `incident.acknowledged`, `incident.escalated`, `incident.mitigated`, `incident.resolved`. The Service Bus message carries `type` also as application property `eventType` for subscription filters.

| Entity | Kind | Consumer |
| --- | --- | --- |
| `incident-events` | topic | |
| `incident-events/sla-scheduler` | subscription, filter `eventType IN ('incident.triggered','incident.escalated')` | Functions `ScheduleSlaCheck` |
| `incident-events/notifier` | subscription, filter `eventType IN ('incident.triggered','incident.escalated')` and `Sev1/Sev2` via property `severity` | Functions `NotifyOnCall` |
| `sla-checks` | queue, messages scheduled at `ackDueAt` | Functions `CheckAcknowledgementSla` |

`sla-checks` message body: `{ "incidentId": "uuid", "escalationLevel": 1 }`. When it fires and the incident is still `Triggered` at the same level, the function calls `POST /api/incidents/{id}/escalate`. The API then emits `incident.escalated` with a new `ackDueAt` (now + ack window), and the cycle repeats until level 3.

`NotifyOnCall` posts `{ incidentNumber, title, severity, serviceId, escalationLevel, target, url }` to the Logic App HTTP trigger, which fans out to the configured channel (Teams/Slack incoming webhook, email).

## Alert ingestion

Alerts become incidents. Both endpoints authenticate with the API key, accepted as `X-Api-Key`, `Authorization: Bearer <key>` or query `code=<key>` (Azure Monitor webhooks cannot send custom headers).

| Method | Path | Payload |
| --- | --- | --- |
| POST | `/api/alerts/alertmanager` | Prometheus Alertmanager webhook, version 4 |
| POST | `/api/alerts/azure-monitor` | Azure Monitor common alert schema |

Rules, implemented in the domain:

- Fingerprint: Alertmanager `fingerprint`; Azure Monitor `essentials.alertRule` + first `alertTargetIDs` entry.
- Firing with no open incident for the fingerprint: create an incident (`source` set, `title` from `annotations.summary` / `essentials.alertRule`, `description` from `annotations.description` / `essentials.description`).
- Firing with an open incident for the fingerprint: append an `Alert` timeline entry, no new incident.
- Resolved: append an `Alert` timeline entry; if the incident is `Triggered` or `Acknowledged`, move it to `Mitigated` with actor `alerting`. Resolution with root cause stays a human action.
- Service: label `service` / `customProperties.service`; unknown or missing service falls back to `platform`, a seventh seeded service.
- Severity mapping:

| Alertmanager `severity` label | Azure Monitor `essentials.severity` | Incident |
| --- | --- | --- |
| `critical` | `Sev0`, `Sev1` | `Sev1` |
| `high`, `error` | `Sev2` | `Sev2` |
| `warning` | `Sev3` | `Sev3` |
| `info`, missing | `Sev4` | `Sev4` |

The API exposes Prometheus metrics at `/metrics` (OpenTelemetry Prometheus exporter): HTTP request rate, error rate and latency histograms, open incidents by severity, SLA breaches. With `Chaos:Enabled=true` (local only) `POST /api/chaos/faults { errorRate, latencyMs, durationSeconds }` injects failures so alerts fire.

Local alerting: Prometheus scrapes the API, evaluates rules (`ApiHighErrorRate`, `ApiHighLatencyP95`, `ApiDown`, `SlaBreachesOpen`) and sends to Alertmanager, which posts to `/api/alerts/alertmanager` with grouping, inhibition (`ApiDown` inhibits the other API alerts) and repeat interval.

Azure alerting: Application Insights standard availability test on `/health/live`, alert rules on failed request rate, server response time, availability, Service Bus dead-lettered messages and Function failures, all routed to an Action Group whose webhook targets `/api/alerts/azure-monitor?code=<key>` with the common alert schema enabled.

## Insights API

Base URL: `https://incidents-insights.marceloroman.com.br`. Reads from `GET /api/incidents/export`.

| Method | Path | Result |
| --- | --- | --- |
| GET | `/api/kpis?days=90` | MTTA, MTTR, SLA compliance per service and severity, weekly trend |
| GET | `/api/recurring?days=180` | clusters of similar incidents (TF-IDF + clustering) with count, services, sample titles |
| GET | `/api/anomalies?days=180` | weeks with anomalous incident volume per service |
| POST | `/api/rca/draft` `{ incidentId }` | AI-drafted RCA (summary, timeline, contributing factors, action items) from Azure OpenAI |
| GET | `/health` | health check |

## Azure topology

Region `eastus2`, resource group `rg-incident-ops`.

| Resource | SKU | Hosts |
| --- | --- | --- |
| Log Analytics + Application Insights | pay-as-you-go | telemetry for every module |
| Container Apps environment (consumption) | | `ca-incident-ops-api` (`services/api`), `ca-incident-ops-insights` (`services/insights`) |
| Azure SQL Database | Basic, 5 DTU; exists only while the environment is powered on | API data |
| Azure SignalR Service | Free_F1 | real-time hub |
| Service Bus namespace | Standard; exists only while the environment is powered on | topic, subscriptions, queue |
| Function App | Flex Consumption, .NET 8 isolated | `func-incident-ops` (`services/functions`) |
| Logic App | Consumption | notifications |
| Azure OpenAI | S0, deployment `rca-drafts` (`gpt-5.4-mini`, DataZoneStandard, parameterized) | RCA drafts |
| Static Web App | Free | `apps/web` |

Managed identities and RBAC over connection strings wherever the service supports it. DNS lives in Cloudflare:

| Host | Target |
| --- | --- |
| `incidents.marceloroman.com.br` | Static Web App |
| `incidents-api.marceloroman.com.br` | Container App `ca-incident-ops-api` |
| `incidents-insights.marceloroman.com.br` | Container App `ca-incident-ops-insights` |
| `incidents-docs.marceloroman.com.br` | GitHub Pages of `marcelo-roman/incident-ops`, built from `docs/` |

### Runtime configuration

Environment variable names injected by the infrastructure. Every module reads exactly these names.

| Consumer | Variable | Value |
| --- | --- | --- |
| API | `ConnectionStrings__IncidentOps` | Azure SQL with `Authentication=Active Directory Managed Identity` |
| API | `ServiceBus__FullyQualifiedNamespace`, `ServiceBus__TopicName` | namespace host, `incident-events` |
| API | `ServiceBus__ConnectionString` | local emulator only |
| API | `Azure__SignalR__ConnectionString` | `Endpoint=https://<signalr>;AuthType=azure.msi;Version=1.0;` |
| API | `Security__EscalationApiKey` | the API key for escalate and alert ingestion |
| API | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1` | web origins |
| API, insights | `APPLICATIONINSIGHTS_CONNECTION_STRING`, `OTEL_SERVICE_NAME` | shared component, `incident-ops-api` / `incident-ops-insights` |
| Insights | `INCIDENTS_API_BASE_URL` | API base URL |
| Insights | `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_DEPLOYMENT` | account endpoint, `rca-drafts` (GPT-5 family: `max_completion_tokens`, no `temperature`) |
| Insights | `CORS_ALLOWED_ORIGINS` | comma-separated web origins |
| Functions | `AzureWebJobsStorage__accountName` | identity-based host storage |
| Functions | `ServiceBusConnection__fullyQualifiedNamespace` | identity-based trigger connection (`ServiceBusConnection` connection string locally) |
| Functions | `IncidentEventsTopic`, `SlaSchedulerSubscription`, `NotifierSubscription`, `SlaChecksQueue` | entity names for `%binding%` expressions |
| Functions | `IncidentsApi__BaseUrl`, `IncidentsApi__ApiKey` | API base URL, API key |
| Functions | `Notifications__LogicAppUrl` | Logic App trigger callback URL |
| Functions | `Web__BaseUrl` | console base URL for incident links |

## Delivery

GitHub Actions is the delivery system. The Azure DevOps YAML pipelines in `infra/azure-devops/` are equivalent samples and are not wired to any project.

- One workflow per module in `.github/workflows/`: `api.yml`, `functions.yml`, `insights.yml`, `web.yml`, `infra.yml`, `docs.yml`, plus `platform.yml` for the compose stack, alert rules, Markdown links, shell scripts and workflow lint. Each workflow filters `on.pull_request.paths` and `on.push.paths` to its module folder and its own workflow file; the four application workflows also run on pull requests that change `contracts/**`. Steps run with `defaults.run.working-directory` set to the module.
- Authentication: `azure/login@v2` with OpenID Connect. One federated credential set for the single repository: `repo:marcelo-roman@195764956/incident-ops@1401969545:environment:production` (deploy jobs), `repo:marcelo-roman@195764956/incident-ops@1401969545:ref:refs/heads/main` (infra validate and what-if on `main`); no `pull_request` subject, so pull requests never obtain an Azure token. Repository variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`; no stored Azure secret.
- Build, lint and tests run on pull requests and on pushes to `main`. Integration into `main` happens only through squash-merged pull requests. Deploy jobs run only on pushes to `main`, require the repository variable `DEPLOY_ENABLED=true`, and target the GitHub environment `production`, which accepts deployments from `main` only.
- Container apps (`ca-incident-ops-api`, `ca-incident-ops-insights`): image pushed to GHCR, then the local reusable workflow `./.github/workflows/deploy-container-app.yml` with inputs `container-app-name`, `resource-group`, `image`, `health-url`; it updates the revision, smoke tests the health URL and rolls back to the previous revision on failure.
- Functions: `Azure/functions-action@v1` (Flex Consumption) to `func-incident-ops`.
- Web: `Azure/static-web-apps-deploy@v1` with secret `SWA_DEPLOYMENT_TOKEN`.
- Docs: `mkdocs build --strict` on pull requests, GitHub Pages deploy on `main`.
- Infra: Bicep build and lint, validate and what-if on pull requests, what-if published to the job summary, deploy on `main` behind the `production` environment.
- Review: `.github/CODEOWNERS` assigns an owner per top-level path; pull requests that change `contracts/` require that owner's review.

## Conventions

- Code in English. No code comments. Names and structure carry the meaning.
- Early return; no `else` after `return`. Ternary only when strictly necessary.
- One responsibility per type. Short functions.
- Domain rules live in the domain layer; endpoints/handlers only bind input, call the application layer and map the result.
- READMEs describe what exists and how to use it, with a table of contents; no history sections, no marketing adjectives.
- Every module ships build, lint and tests, run in CI on every pull request.
- OpenTelemetry instrumentation exporting to Application Insights; structured logs with correlation ids.
- Dockerfile per deployable, non-root, health check.
- Configuration via environment variables; no secrets in the repository.
