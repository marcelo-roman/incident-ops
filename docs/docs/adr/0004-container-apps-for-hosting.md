---
status: accepted
date: 2026-09-09
deciders: Marcelo Roman
---

# Azure Container Apps over App Service and AKS

## Context and problem statement

Two HTTP services need hosting: the Incidents API (`services/api`, .NET 8) and Insights (`services/insights`, Python). Traffic is bursty and low: idle most of the day, spikes during incidents. Where do they run?

## Decision drivers

- Scale to zero when idle; pay per use.
- Same container image locally, in CI and in production.
- HTTPS with custom domains and managed certificates.
- Operable by a team without a dedicated platform engineer.

## Considered options

1. Azure Container Apps, consumption environment
2. App Service (Linux, containers), Basic B1 per app
3. AKS

## Decision outcome

Chosen option: **Container Apps**, one consumption environment hosting both apps, `minReplicas: 0` and HTTP concurrency scale rules on both.

### Consequences

- Good: idle cost near zero; HTTP-based autoscaling built in.
- Good: revisions give blue/green with traffic splitting for releases ([release readiness](../engineering/release-readiness-checklist.md)).
- Good: managed identity, custom domains and managed certificates without cluster operations.
- Bad: cold start after scale-to-zero (2–5 s for the .NET API). Accepted; the availability test keeps it warm during business hours.
- Bad: less control than Kubernetes (no custom operators, limited networking options on consumption).

## Pros and cons of the options

### App Service

- Good: mature, deployment slots, simple.
- Bad: no scale to zero; two always-on plans for low traffic.

### AKS

- Good: maximum control; fits a platform with dozens of services.
- Bad: cluster upgrades, node pools, ingress and certificates are a standing workload, which is unjustified for two services. Revisit above roughly 15 services or with requirements Container Apps cannot meet.
