# Insights API

KTLO analytics and AI-assisted root cause analysis for the Incident Ops system. It reads incidents from the [Incidents API](../api) (`GET /api/incidents/export`), computes response and SLA metrics, finds recurring issues and volume anomalies, and drafts RCAs with Azure OpenAI.

## Contents

1. [What it does](#what-it-does)
2. [Architecture](#architecture)
3. [Methods](#methods)
4. [HTTP API](#http-api)
5. [Authentication](#authentication)
6. [CLI](#cli)
7. [Running locally](#running-locally)
8. [Configuration](#configuration)
9. [Testing and quality](#testing-and-quality)
10. [Deployment](#deployment)
11. [License](#license)

## What it does

| Capability | Where |
| --- | --- |
| MTTA and MTTR (median, p90) per service and severity, SLA compliance, detection coverage, weekly trend | `GET /api/kpis` |
| Clusters of similar incidents from their title and description | `GET /api/recurring` |
| Weeks with abnormal incident volume per service | `GET /api/anomalies` |
| RCA draft from an incident and its timeline, labelled with the drafter that produced it | `POST /api/rca/draft` |
| Weekly KTLO report in Markdown or HTML | `incident-insights report` |
| Deterministic sample history for offline work | `incident-insights generate-sample`, `data/sample_incidents.csv` |
| Exploratory analysis with executed outputs | `notebooks/ktlo_exploration.ipynb` |

## Architecture

The service is one bounded context, **Operational Analytics**: it reads incidents owned by the Incidents API and turns them into operational measures. It never changes an incident. The upstream shapes are fixed by the [contract](../../contracts/contracts.md#insights-api).

### Domain model

The domain is plain Python (frozen dataclasses and enums, no Pydantic, no I/O). Behaviour lives on the types that own the data:

| Type | Kind | Behaviour |
| --- | --- | --- |
| `IncidentRecord` | entity (identity: incident id, immutable) | `time_to_acknowledge()`, `time_to_resolve()`, `acknowledgement_outcome()`, `resolution_outcome(as_of)`, `sla_outcome(as_of)`, `complies_with_sla()`, `is_settled(as_of)`, `detected_by_monitoring()`, `created_off_hours()`, `week_start()`, `chronology()` (recorded timeline, or one derived from the lifecycle timestamps) |
| `IncidentHistory` | first-class collection | `within(window)`, `by_service()`, `by_severity()`, `by_source()`, `by_responder()`, `in_week()`, `weekly_volume(window)`, `find(id)` |
| `Severity`, `ServiceId`, `DetectionSource`, `IncidentStatus` | value objects | `ServiceId` validates the slug; `Severity.is_high`; `DetectionSource.is_monitoring` |
| `ReportingWindow` | value object | half-open interval, `trailing(days, as_of)`, `contains()`, `week_starts()`; rejects naive or empty windows |
| `Percentage`, `DurationStats` | value objects | `Percentage.share()`, `from_fraction()` with range check; `DurationStats.of_minutes()` computes median and p90 |
| `SlaTargets`, `SlaPolicy`, `SlaOutcome`, `SlaState` | value objects | the contract's SLA table, outcome judgement, and the live `slaState` rule |
| `BusinessHours` | value object | which moments are off-hours for the on-call rotation |
| `Cluster`, `RecurringReport`, `VolumeAnomaly`, `AnomalyReport`, `KpiReport`, `KtloReport` | value objects | analysis results with invariants (a cluster has terms and members, a breach names a missed target, ...) |
| `RcaDraft`, `ActionItem`, `Priority` | value objects | non-empty summary and impact, action items with a title, an owner and a `Priority` derived from severity |

Domain services hold behaviour that spans many records. They take and return domain types; pandas, NumPy and scikit-learn appear only inside them as computation detail:

| Service | Responsibility |
| --- | --- |
| `KpiCalculator` | MTTA/MTTR statistics, SLA compliance, detection coverage, groupings and the weekly trend (pandas for the per-record facts) |
| `RecurringIssueDetector` | TF-IDF, LSA and k-means clustering of incident text (scikit-learn) and cluster description |
| `VolumeAnomalyDetector` | rolling z-score over weekly volume (NumPy) |
| `RcaDraftComposer` | the deterministic RCA draft built from the record |
| `KtloReportComposer` | the weekly KTLO report: breaches, on-call load, top recurring issues, slowest services |

### Layers

```text
src/incident_insights/
  domain/            Operational Analytics model
    shared/          Severity, ServiceId, DetectionSource, ReportingWindow, Percentage, DurationStats
    incidents/       IncidentRecord, IncidentHistory, SLA policy and outcomes, timeline, business hours
    kpis/            KPI value objects and KpiCalculator
    recurring/       Cluster value objects and RecurringIssueDetector
    anomalies/       VolumeAnomaly value objects and VolumeAnomalyDetector
    rca/             RcaDraft value objects and RcaDraftComposer
    reports/         KTLO report value objects and KtloReportComposer
  application/       use cases (GetKpis, FindRecurringIssues, DetectVolumeAnomalies, DraftRca,
                     BuildKtloReport), ports (IncidentSource, RcaDrafter, Clock) and the use-case
                     outputs: camelCase response models built from domain results
  infrastructure/    adapters: anti-corruption layer for the Incidents API payload, API (httpx,
                     retries, TTL cache) and CSV sources, Azure OpenAI and deterministic RCA
                     drafters, JWT verification (PyJWT), settings, sample generator, logging and
                     telemetry, composition root
  interface/         FastAPI app, routers and the bearer authentication dependency, Typer CLI,
                     report rendering (Jinja)
```

Use cases only orchestrate: load the history for a window through the `IncidentSource` port, call a domain service, and return its output model. The anti-corruption layer (`infrastructure/incidents/acl.py`) is the only place that knows the API's field names (`serviceId`, `ackDueAt`, ...); it translates the export and the CSV rows into `IncidentRecord` and back. The Azure OpenAI structured-output schema lives in the adapter and is mapped to `RcaDraft`, so a model answer that breaks the draft's invariants is rejected.

The boundaries are [import-linter](https://import-linter.readthedocs.io/) contracts in `pyproject.toml`, checked by `uv run lint-imports` in CI:

| Contract | Rule |
| --- | --- |
| Layers | `interface` → `infrastructure` → `application` → `domain`; never upwards |
| No I/O frameworks inside | `domain` and `application` do not import `httpx`, `openai`, `azure`, `fastapi`, `jwt`, `typer`, `jinja2`, `pydantic_settings`, `opentelemetry` |
| Plain domain | `domain` does not import `pydantic` |
| Computation stays in services | entities, collections and value-object modules do not import `pandas`, `sklearn`, `scipy` |
| Routers use use cases | `interface.api.routers` does not import `infrastructure` |
| Interface sees outputs | `interface` does not import the domain model; it only uses `application` outputs and the shared duration formatting |
| Independent features | `kpis`, `recurring`, `anomalies` and `rca` do not import each other |

## Methods

### Incident measures

Each `IncidentRecord` answers these questions itself, evaluated at the end of the window (`as_of`):

| Measure | Definition |
| --- | --- |
| time to acknowledge (MTTA) | `acknowledgedAt - createdAt`; empty while unacknowledged |
| time to resolve (MTTR) | `resolvedAt - createdAt`; empty while unresolved |
| acknowledgement outcome | `Breached` if `acknowledgementBreached` is true or the incident was resolved without an acknowledgement; `Met` if acknowledged without a breach; `Pending` while open and unacknowledged |
| resolution outcome | `Met` if resolved by `resolveDueAt`; `Breached` if resolved later or `resolveDueAt` has passed while open; `Pending` otherwise |
| SLA outcome | `Breached` if either target breached, `Met` if both met, `Pending` otherwise |
| week | Monday 00:00 UTC of `createdAt` |

The window is `[as_of - days, as_of)` on `createdAt`. An incident created before the window and resolved inside it is not counted.

### MTTA and MTTR

Median and 90th percentile (linear interpolation) of the minutes above, with the number of samples. Response times are right-skewed, so means would be dominated by a few long incidents; the p90 shows the tail. Open incidents are left out of MTTR until they resolve, which makes MTTR optimistic for the most recent days.

### SLA compliance

Percentages of `Met` among decided outcomes (`Met` + `Breached`), reported per target (`acknowledgePct`, `resolvePct`) and overall (`overallPct`, both targets met). `Pending` incidents are excluded from the denominator. Targets come from the shared SLA policy (Sev1: 15 min / 4 h, Sev2: 30 min / 8 h, Sev3: 4 h / 3 days, Sev4: 24 h / 10 days).

This follows the shared contract: an incident complies when it was acknowledged, `acknowledgementBreached` is false, and it was resolved by `resolveDueAt`. The acknowledgement is never compared with `ackDueAt`, because every escalation moves `ackDueAt` to a new window; the API sets `acknowledgementBreached` on escalation or late acknowledgement and never resets it. MTTA stays `acknowledgedAt - createdAt`.

### Detection coverage

Share of incidents whose `source` is `Alertmanager` or `AzureMonitor` (`alertingPct`), plus incidents, share and MTTA per source. A low share means customers or engineers notice problems before monitoring does. MTTA per source shows whether alert-driven pages are picked up faster than manually opened incidents.

### Weekly trend

One row per week in the window, including weeks without incidents: count, median MTTA, median MTTR and overall SLA compliance. The first and last weeks are usually partial.

### Recurring issues

`RecurringIssueDetector` (`domain/recurring/`):

1. Documents are `title + ". " + description`.
2. TF-IDF with English stop words, unigrams and bigrams, sublinear term frequency, tokens that start with a letter (numbers such as latencies and counts are dropped), `min_df=2`, and `max_df=0.5` once there are 20 or more incidents.
3. Truncated SVD to at most 100 dimensions, then L2 normalisation (latent semantic analysis). On normalised vectors, Euclidean k-means is equivalent to clustering by cosine similarity, and the reduction makes it fast.
4. K-means for every k from 2 to `min(25, n - 1)` with `random_state=42` and 5 restarts; the k with the highest cosine silhouette score wins.
5. Each cluster is described in TF-IDF space: its centroid ranks the terms, the label is the top three terms that are not covered by a higher-ranked phrase, preferring terms that appear in the titles; `sampleTitles` are the distinct titles closest to the centroid; `cohesion` is the mean cosine similarity of the members to the centroid.
6. Clusters with fewer than 2 incidents are dropped. The result is deterministic for the same input.

Limits: the silhouette favours tight clusters, so on short windows the same issue can split by a distinctive token (for example a region name); a shared issue reported on several services (connection pool exhaustion) lands in one cluster with several services, which is the intended reading. Fewer than 3 incidents, or text with no usable vocabulary, returns no clusters. Clustering cost grows with the number of candidate k values; with thousands of incidents per window an incremental approach (MiniBatchKMeans, a narrower k range) would be needed.

### Volume anomalies

`VolumeAnomalyDetector` (`domain/anomalies/`), implemented with NumPy (`sliding_window_view`):

1. Weekly incident counts per service, with zero for weeks without incidents.
2. For each week, the baseline is the mean and population standard deviation of the previous 8 weeks (the current week is excluded). At least 4 previous weeks are required.
3. `z = (count - mean) / max(std, 1.0)`. The floor of 1 incident keeps a near-constant baseline from turning a small change into a large z-score.
4. A week is flagged when `z >= 3` and the count is at least 3.

Only increases are flagged. The first 4 weeks of the window have no baseline, so a 180-day window evaluates about 22 weeks. A sustained increase raises the baseline and stops being flagged after a few weeks, which is the expected behaviour for a level shift. Seasonality is not modelled; IsolationForest or a seasonal decomposition would be the next step if weekly patterns become relevant.

### SLA breaches and on-call load (report only)

Breaches list every incident with `sla_outcome = Breached`, ordered by severity and then most recent, with the breached targets and the resolve overrun (to `resolvedAt`, or to `as_of` while open). On-call load groups by assignee: incidents, Sev1/Sev2 incidents, incidents created off-hours (outside 09:00-18:00 America/New_York or on weekends, matching the rotation's time zone) and incidents that escalated past level 1.

### RCA drafts

`POST /api/rca/draft` loads the incident with its timeline from the source and sends it to a provider:

- `AzureOpenAIRcaDrafter` (adapter for the `RcaDrafter` port) uses the `openai` SDK's `AzureOpenAI` client with a Microsoft Entra ID token from `DefaultAzureCredential` (managed identity in Azure, developer credentials locally), API version `2025-04-01-preview`, and `chat.completions.parse` with the `RcaDraft` Pydantic model as a strict JSON schema (`response_format` `json_schema`). The deployment (`rca-drafts`) runs a GPT-5 family model (`gpt-5.4-mini`), a reasoning model: the request sets `max_completion_tokens` (8000, which also covers reasoning tokens) and sends no `temperature` or `top_p`. The system prompt treats incident text as data, asks the model not to invent causes and to say what must be confirmed. `generatedBy` is `azure-openai:<deployment>`.
- `DeterministicRcaDrafter` delegates to the `RcaDraftComposer` domain service and is used when `AZURE_OPENAI_ENDPOINT` is not set. It builds the draft from the record: the timeline, the recorded root cause, the acknowledgement breach flag, escalation, late resolution and manual detection, with action items prioritised from the severity. `generatedBy` is `deterministic-fallback`.

A model refusal, a truncated answer (`finish_reason: length`) or an Azure OpenAI error returns `502` instead of silently switching to the fallback, so a draft is never mislabelled. The draft is a starting point for the incident review, not a finding.

## HTTP API

Base URL `https://incidents-insights.marceloroman.com.br`. JSON in camelCase. Errors are RFC 7807 `application/problem+json`. Every response carries `X-Correlation-Id` (echoed from the request or generated). OpenAPI at `/docs`. Every `/api/*` endpoint requires a bearer token (see [Authentication](#authentication)); `/health`, `/docs` and `/openapi.json` are anonymous.

| Method | Path | Result |
| --- | --- | --- |
| GET | `/api/kpis?days=90` | `window`, `overall`, `detection`, `bySeverity`, `byService`, `byServiceSeverity`, `weekly` |
| GET | `/api/recurring?days=180` | `incidentsAnalyzed`, `clustersEvaluated`, `silhouette`, `clusters[]` with `label`, `terms`, `count`, `cohesion`, `services`, `sampleTitles`, `firstSeen`, `lastSeen` |
| GET | `/api/anomalies?days=180` | `method`, `baselineWeeks`, `threshold`, `anomalies[]` with `serviceId`, `weekStart`, `count`, `baselineMean`, `baselineStd`, `zScore` |
| POST | `/api/rca/draft` `{ "incidentId": "uuid" }` | `summary`, `impact`, `timeline[]`, `contributingFactors[]`, `actionItems[{title, owner, priority}]`, `generatedBy`, `generatedAt`, `incidentId`, `incidentNumber` |
| GET | `/health` | `{ "status": "ok" }`; liveness only, does not call the Incidents API |

`days` accepts 1 to 730. Status codes: `401` missing, invalid or expired token, `404` unknown incident, `422` invalid input, `502` Incidents API or Azure OpenAI failure.

```bash
TOKEN=$(curl -s -X POST https://incidents-api.marceloroman.com.br/api/auth/token \
  -H 'Content-Type: application/json' -d '{"username":"<user>","password":"<password>"}' | jq -r '.accessToken')
curl -s "https://incidents-insights.marceloroman.com.br/api/kpis?days=30" \
  -H "Authorization: Bearer $TOKEN" | jq '.overall'
curl -s -X POST https://incidents-insights.marceloroman.com.br/api/rca/draft \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"incidentId":"<uuid>"}' | jq '.generatedBy, .actionItems'
```

CORS allows `https://incidents.marceloroman.com.br` and `http://localhost:5173`, with the `Authorization`, `Content-Type` and `X-Correlation-Id` request headers.

## Authentication

Insights does not issue tokens. It accepts the access token issued by the Incidents API (`POST /api/auth/token`) and validates it locally with the shared signing key:

| Check | Value |
| --- | --- |
| Algorithm | `HS256` only |
| Key | `AUTH_SIGNING_KEY`, at least 32 bytes; the same value as the API's `Auth__SigningKey` |
| Issuer | `incident-ops-api` |
| Audience | `incident-ops` |
| Required claims | `exp`, `iss`, `aud`, `sub` |
| Clock skew | 30 seconds |

`interface/api/authentication.py` is a FastAPI dependency attached to every `/api` router in `create_app`; routers and use cases do not know about it. The verifier (`infrastructure/security/token_verifier.py`) is the only module that imports PyJWT. A missing token, another scheme, a bad signature, an expired token or a wrong issuer or audience returns `401` with `WWW-Authenticate: Bearer` and a problem body that does not say which check failed. OpenAPI declares the `HTTPBearer` scheme, so **Authorize** in `/docs` accepts a token.

`create_app` refuses to start when `AUTH_SIGNING_KEY` is missing or shorter than 32 bytes. The CLI does not read it.

Calls from Insights to the Incidents API send the service API key in `X-Api-Key` (`INCIDENTS_API_KEY`). Without it the API answers `401`, which surfaces as `502`.

For a manual call without the API, sign a token with the same key:

```bash
uv run python -c "import jwt, time; print(jwt.encode({'sub': 'demo', 'name': 'demo', 'iss': 'incident-ops-api', 'aud': 'incident-ops', 'exp': int(time.time()) + 3600}, '<AUTH_SIGNING_KEY>', algorithm='HS256'))"
```

## CLI

```bash
uv run incident-insights report --days 30 --format md
uv run incident-insights report --days 7 --format html --output reports/ktlo.html
uv run incident-insights report --source csv --days 30 --as-of 2026-09-28
uv run incident-insights generate-sample
```

| Command | Options |
| --- | --- |
| `report` | `--days` (default 30), `--format md\|html`, `--source api\|csv` (default `INSIGHTS_DATA_SOURCE`), `--csv-path`, `--as-of` (ISO-8601, default now), `--output` (default stdout) |
| `generate-sample` | `--output` (default `data/sample_incidents.csv`), `--end` (default 2026-09-28), `--days` (default 182), `--seed` (default 42) |

The report has a headline, detection by source, top 5 recurring issues, SLA breaches (15 most severe), MTTR by service (slowest first) and on-call load. Excerpt from `report --source csv --days 30 --as-of 2026-09-28`:

```markdown
## Headline

| Incidents | Detected by monitoring | SLA compliance | Acknowledge SLA | Resolve SLA | MTTA median | MTTA p90 | MTTR median | MTTR p90 |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 65 | 56.9% | 65.6% | 76.6% | 85.2% | 1h 10m | 7h 28m | 18h 17m | 3d 22h 32m |

## Detection

| Source | Incidents | Share | MTTA median | MTTA p90 |
|---|---:|---:|---:|---:|
| Manual | 28 | 43.1% | 2h 39m | 17h 40m |
| Alertmanager | 20 | 30.8% | 40m | 2h 19m |
| AzureMonitor | 17 | 26.2% | 31m | 5h 12m |

## Top recurring issues

| Cluster | Incidents | Services | Example |
|---|---:|---|---|
| acquirer authorization / authorization timeouts / card authorization | 6 | payments-gateway (6) | Card authorization timeouts with acquirer |
| push notifications / android devices / failing android | 6 | notifications (6) | Push notifications failing for Android devices |
| error rate / deployment / rate spike | 5 | checkout (1), identity (1), notifications (1), reporting (1), search (1) | Error rate spike after deployment of search |

## SLA breaches

21 incidents breached at least one SLA target; the 15 most severe are listed.

| Incident | Severity | Service | Status | Breached | Resolve overrun | Assignee |
|---|---|---|---|---|---:|---|
| INC-1391 Duplicate charges reported by customers | Sev1 | payments-gateway | Resolved | acknowledge, resolve | 42m | alice.nguyen |
| INC-1366 Card authorization timeouts with acquirer | Sev1 | payments-gateway | Resolved | acknowledge | n/a | erin.walsh |
```

### Sample data

`data/sample_incidents.csv` is produced by `generate-sample` and committed so tests, the notebook and the CSV source work offline. It holds 26 weeks of incidents for the seven services, generated from issue families with fixed severity mixes and alerting likelihood, lognormal acknowledge and resolve times relative to each severity's SLA targets, escalation that moves `ackDueAt` one window per level (up to level 3) and sets `acknowledgementBreached`, the weekly on-call rotation, and two injected volume spikes (`payments-gateway` and `search`). The same seed always produces the same file; a test checks that the committed file matches the generator.

## Running locally

Requirements: Python 3.12 and [uv](https://docs.astral.sh/uv/).

The full stack runs from the [root compose file](../../docker-compose.yml) (`cp .env.example .env && docker compose up --build` from the repository root); Insights is then on <http://localhost:8000> (`INSIGHTS_PORT`) and reads the local API. To run this module on its own:

```bash
uv sync
AUTH_SIGNING_KEY=local-signing-key-at-least-32-bytes INSIGHTS_DATA_SOURCE=csv uv run uvicorn incident_insights.interface.api.app:create_app --factory --reload
```

Against the deployed Incidents API instead of the sample, omit `INSIGHTS_DATA_SOURCE` and set `INCIDENTS_API_KEY`. To use Azure OpenAI, sign in with `az login` (or any `DefaultAzureCredential` source) with the `Cognitive Services OpenAI User` role on the resource and set `AZURE_OPENAI_ENDPOINT`.

With Docker:

```bash
docker build -t incident-insights .
docker run --rm -p 8000:8000 -e INSIGHTS_DATA_SOURCE=csv \
  -e AUTH_SIGNING_KEY=local-signing-key-at-least-32-bytes incident-insights
```

The image is a two-stage build on `python:3.12-slim`, runs uvicorn as a non-root user (uid 10001) and declares a `HEALTHCHECK` on `/health`. It includes the sample CSV.

Notebook:

```bash
uv sync --group notebook
uv run jupyter nbconvert --execute --inplace notebooks/ktlo_exploration.ipynb
```

## Configuration

Environment variables (a `.env` file is also read; see `.env.example`). `INCIDENTS_API_BASE_URL`, `INCIDENTS_API_KEY`, `AUTH_SIGNING_KEY`, `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_DEPLOYMENT`, `CORS_ALLOWED_ORIGINS`, `APPLICATIONINSIGHTS_CONNECTION_STRING` and `OTEL_SERVICE_NAME` are the names injected by the infrastructure; the others are local tuning with working defaults.

| Variable | Default | Purpose |
| --- | --- | --- |
| `INCIDENTS_API_BASE_URL` | `https://incidents-api.marceloroman.com.br` | Incidents API base URL |
| `INCIDENTS_API_KEY` | unset | Service API key sent as `X-Api-Key` to the Incidents API |
| `AUTH_SIGNING_KEY` | required by the HTTP API | HS256 key that validates bearer tokens, at least 32 bytes |
| `INCIDENTS_API_TIMEOUT_SECONDS` | `15` | HTTP timeout for the Incidents API |
| `INCIDENTS_API_ATTEMPTS` | `3` | Attempts per Incidents API request (1 disables retries) |
| `INSIGHTS_DATA_SOURCE` | `api` | `api` or `csv` |
| `INSIGHTS_CSV_PATH` | `data/sample_incidents.csv` | CSV file for the `csv` source |
| `INSIGHTS_CACHE_TTL_SECONDS` | `300` | TTL of the export cache for the `api` source |
| `CORS_ALLOWED_ORIGINS` | `https://incidents.marceloroman.com.br,http://localhost:5173` | Comma-separated allowed origins |
| `AZURE_OPENAI_ENDPOINT` | unset | Azure OpenAI account endpoint; when set, RCA drafts use Azure OpenAI |
| `AZURE_OPENAI_DEPLOYMENT` | `rca-drafts` | Model deployment name (GPT-5 family) |
| `AZURE_OPENAI_API_VERSION` | `2025-04-01-preview` | Azure OpenAI data-plane API version |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | unset | Enables Azure Monitor OpenTelemetry |
| `OTEL_SERVICE_NAME` | `incident-ops-insights` | Service name on telemetry |
| `LOG_LEVEL` | `INFO` | Log level |

Calls to the Incidents API are GETs and are retried on HTTP 5xx, timeouts and connection errors, with exponential backoff (0.2 s doubling, capped at 2 s) and full jitter (`infrastructure/incidents/retry.py`). Other status codes are not retried. When the attempts are exhausted the request fails with a `502` problem response.

The export cache widens each request to whole hours, keeps the result for the TTL and filters it back to the requested window, so repeated dashboard calls within the same hour hit the Incidents API once per TTL. Incident detail lookups for RCA drafts are not cached.

Observability: logs are JSON on stdout with `correlationId`, and `traceId`/`spanId` when a span is active. With `APPLICATIONINSIGHTS_CONNECTION_STRING` set, `configure_azure_monitor()` exports traces, metrics and logs of the `incident_insights` loggers, with FastAPI and outgoing httpx calls instrumented.

## Testing and quality

```bash
uv run ruff check .
uv run ruff format --check .
uv run mypy
uv run lint-imports
uv run pytest
```

Tests mirror the layers:

| Folder | What is tested | How |
| --- | --- | --- |
| `tests/domain` | value objects and their invariants, `IncidentRecord` behaviour, `IncidentHistory`, `KpiCalculator`, `RecurringIssueDetector`, `VolumeAnomalyDetector`, `RcaDraftComposer`, `KtloReportComposer` | small hand-built incident sets with known answers (percentiles, SLA outcomes, z-scores, cluster membership) |
| `tests/application` | use cases | in-memory incident source, canned and failing drafters, fixed clock |
| `tests/infrastructure` | anti-corruption layer mapping, Incidents API client (API key header) and retries, JWT verification (signature, expiry, issuer, audience, required claims, key length), CSV source, cache, settings, Azure OpenAI drafter, logging, telemetry | `httpx.MockTransport`; the real `AzureOpenAI` client over a mocked transport, asserting the structured-output request, `max_completion_tokens`, no sampling parameters, and the refusal, truncation, invalid-draft and error paths |
| `tests/interface` | HTTP API, bearer authentication (`401` paths, anonymous health and docs, OpenAPI scheme, startup without a key), CLI, report rendering, snapshots | `TestClient` with use cases over fakes and tokens signed by `tests/tokens.py`; `CliRunner` against the committed sample |

`tests/snapshots` holds the responses of `/api/kpis`, `/api/recurring`, `/api/anomalies`, five RCA drafts and the Markdown and HTML reports, all computed on the committed sample with a fixed clock. `tests/interface/test_snapshots.py` compares the current output with them, so a refactoring that changes a number or a field fails the build.

`mypy --strict` covers `src` and `tests`. Coverage is reported on every run (99% at the time of writing) and the run fails under 85%.

## Deployment

GitHub Actions delivers; [`.github/workflows/insights.yml`](../../.github/workflows/insights.yml) runs when `services/insights/**` or the workflow changes (and on pull requests that change `contracts/**`):

| Job | When | Steps |
| --- | --- | --- |
| `verify` | pull requests and pushes to `main` | `uv sync --frozen`, ruff check, ruff format check, mypy, import-linter contracts, pytest with coverage |
| `image` | after `verify` | builds the image; on `main` pushes `ghcr.io/marcelo-roman/incident-ops-insights:<sha>` and `:latest` |
| `deploy` | `main` only | calls the reusable [`deploy-container-app.yml`](../../.github/workflows/deploy-container-app.yml) with `container-app-name: ca-incident-ops-insights`, `resource-group: rg-incident-ops`, the image of the commit and `health-url: https://incidents-insights.marceloroman.com.br/health` |

The reusable workflow authenticates with OpenID Connect (`id-token: write`, repository variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`), runs under the `production` environment, updates the Container App revision, smoke tests the health URL and rolls back on failure. The Container App's managed identity needs `Cognitive Services OpenAI User` on the Azure OpenAI resource; no key is stored.

[`infra/azure-devops/insights.yml`](../../infra/azure-devops/insights.yml) is an equivalent Azure DevOps sample and is not wired to any project. It runs the same checks, pushes the image through a Docker registry service connection named `ghcr-marcelo-roman`, and deploys with the step template `infra/azure-devops/templates/deploy-container-app.yml` (service connection `sc-incident-ops`).

## License

[MIT](../../LICENSE)
