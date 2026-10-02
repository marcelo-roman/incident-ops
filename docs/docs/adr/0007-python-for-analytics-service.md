---
status: accepted
date: 2026-09-11
deciders: Marcelo Roman
---

# Python for the analytics and RCA service

## Context and problem statement

KTLO metrics (MTTA, MTTR, SLA compliance per service and severity), recurring-issue detection (text clustering), volume anomaly detection and LLM-drafted RCAs need a home. Do we add them to the .NET API or build a separate service, and in which language?

## Decision drivers

- Analytics iterate faster than the system of record and should not risk its availability.
- Text clustering and anomaly detection are first-class in the Python ecosystem.
- Engineers doing analytics work are often comfortable in Python and notebooks.

## Considered options

1. Separate Python service: FastAPI, Pandas, NumPy, scikit-learn, Azure OpenAI SDK
2. Endpoints inside the .NET API using ML.NET and LINQ aggregations
3. Scheduled notebooks writing results to a table

## Decision outcome

Chosen option: **separate Python service** reading from `GET /api/incidents/export`. It is stateless; the API stays the only writer.

### Consequences

- Good: Pandas expresses MTTA/MTTR and group-bys in a few lines that analysts can review; scikit-learn gives TF-IDF and clustering without custom code.
- Good: an outage or slow query in Insights never affects incident handling.
- Good: prototype in a notebook, promote to an endpoint with tests.
- Bad: a second runtime to build, scan and patch; covered by the same [quality gates](../engineering/quality-gates.md) (ruff, mypy, pytest).
- Bad: pulling the export per request does not scale to hundreds of thousands of incidents. At that size, move to an incremental store; until then, in-memory caching is enough.

## Pros and cons of the options

### Inside the .NET API

- Good: one deployable.
- Bad: ML.NET text clustering is less mature; analytics changes would redeploy the system of record.

### Scheduled notebooks

- Good: cheapest to start.
- Bad: no API for the console; untested code paths producing numbers leadership relies on.
