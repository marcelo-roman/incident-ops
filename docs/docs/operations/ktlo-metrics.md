# KTLO metrics

What we measure about keeping the lights on, how each number is computed, where it comes from, and what it triggers.


## Metric catalog

| Metric | Target | Source | Endpoint / query |
|---|---|---|---|
| MTTA | Sev1 ≤ 10 min, Sev2 ≤ 20 min | incidents | `GET /api/metrics/summary` (30 d), Insights `GET /api/kpis` |
| MTTR | Sev1 ≤ 2 h, Sev2 ≤ 6 h | incidents | same |
| SLA compliance | Sev1/Sev2 ≥ 95%, Sev3/Sev4 ≥ 90% | incidents | same, per service and severity |
| Breached open | 0 | incidents | `GET /api/metrics/summary` `breachedOpen` |
| Incident volume per service | trend down quarter over quarter | incidents | Insights `GET /api/kpis` weekly trend, `GET /api/anomalies` |
| Recurring-issue rate | ≤ 15% | incidents | Insights `GET /api/recurring` |
| Toil % | ≤ 20% of engineering time | Azure Boards | tag `toil` |
| KTLO vs feature split | planned 25% KTLO ±5 pp | Azure Boards | tags `ktlo`, `tech-debt` |
| Postmortem actions closed on time | ≥ 85% | Azure Boards | tag `incident-followup` |

## Definitions

All durations from incident timestamps in UTC, over incidents created in the period.

| Metric | Formula | Notes |
|---|---|---|
| MTTA | mean(`acknowledgedAt − createdAt`) over acknowledged incidents | Median and p90 reported alongside; one outlier moves a mean a lot at low volume |
| MTTR | mean(`resolvedAt − createdAt`) over resolved incidents | Time to restore is `mitigatedAt − createdAt`, reported as MTTM; resolve includes root cause |
| SLA compliance | count(`resolvedAt ≤ resolveDueAt`) ÷ count(resolved) | Per severity; open incidents past due count as breached in the period they breach |
| Ack compliance | count(acknowledged before first escalation) ÷ count(acknowledged) | `escalationLevel = 1` at acknowledgement |
| Incident volume | count per service per ISO week | Normalized per Tier for comparisons |
| Recurring-issue rate | incidents in a cluster of size ≥ 3 ÷ all incidents, over 180 days | Clusters from `GET /api/recurring` |
| Anomalous week | weekly count per service with robust z-score > 3.5 vs trailing 12 weeks | From `GET /api/anomalies` |
| Toil % | hours on items tagged `toil` ÷ total completed hours in the sprint | Manual, repetitive, automatable, no lasting value |
| KTLO split | points tagged `ktlo` ÷ completed points; same for `tech-debt`; rest is feature | Planned vs actual per sprint |

## How Insights computes them

Insights pulls `GET /api/incidents/export?from=&to=` (flat incidents, no timeline) and works on a Pandas DataFrame.

| Endpoint | Computation |
|---|---|
| `GET /api/kpis?days=90` | Parse timestamps as UTC; `tta = acknowledgedAt − createdAt`, `ttr = resolvedAt − createdAt`; `groupby([serviceId, severity])` → mean/median/p90 in minutes and compliance ratio; weekly trend via `resample("W-MON", on="createdAt")` |
| `GET /api/recurring?days=180` | Text = `title + " " + rootCause`; scikit-learn `TfidfVectorizer` (English stop words, 1–2 grams, `min_df=2`); clustering over cosine distance; clusters of size ≥ 3 returned with count, services and sample titles |
| `GET /api/anomalies?days=180` | Weekly counts per service; median and MAD over a trailing 12-week window; `z = 0.6745 × (x − median) ÷ MAD`; flagged when `z > 3.5` |
| `POST /api/rca/draft` | Incident + timeline to Azure OpenAI; output is a draft input to the [postmortem](postmortem-template.md), not a metric |

Choices worth knowing:

- Median/MAD instead of mean/standard deviation for anomalies: incident counts are small and spiky; one bad week should not hide the next.
- TF-IDF instead of embeddings for recurrence: deterministic, explainable (top terms per cluster), no model cost, good enough on short titles.
- The API's `/api/metrics/summary` covers the fixed 30-day headline numbers for the console; Insights covers breakdowns and trends. Both use the same formulas; snapshot tests in Insights pin them ([quality gates](../engineering/testing-strategy.md#operational-analytics-servicesinsights)).

## Metrics from Azure Boards

Toil, KTLO split and follow-up closure come from work items, not incidents. Queries in [ADO hygiene](../engineering/ado-hygiene.md#queries). They depend on tagging discipline, which the weekly hygiene sweep checks.

## Review cadence

| Forum | Frequency | Metrics | Output |
|---|---|---|---|
| Ops review | weekly, 30 min | open breached, last week's Sev1/Sev2, anomalies, on-call load | actions into the sprint |
| Sprint review | every 2 weeks | KTLO split planned vs actual, toil %, follow-up closure | allocation for next sprint |
| Monthly ops report | monthly | MTTA, MTTR, SLA compliance trend, recurring clusters top 5 | [stakeholder update](../leadership/stakeholder-updates.md) section |
| Quarterly planning | quarterly | all, quarter over quarter | KTLO allocation for the quarter ([KTLO vs roadmap](../leadership/ktlo-vs-roadmap.md)) |

Triggers:

- Recurring cluster with ≥ 5 incidents in 90 days → problem record (a Feature under the KTLO epic) with an owner.
- Toil > 20% two sprints running → automation work prioritized from the KTLO allocation.
- SLA compliance below target for a service two months running → service review with the owning team and Product Owner.

## Pitfalls

- MTTR goes down when teams resolve faster with less rigor. Read it with recurring-issue rate.
- Severity inflation makes compliance look worse; deflation makes it look better. Audit a sample of severities monthly.
- Low volume makes percentages swing. Show counts next to every ratio.
