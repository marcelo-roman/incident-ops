---
status: accepted
date: 2026-10-02
deciders: Marcelo Roman
---

# Demo authentication with a signed JWT and service API keys

## Context and problem statement

Incident Ops is a public portfolio deployment: the console, the Incidents API and the Insights API run on Azure with public endpoints, and the source is public. Anyone who finds the URLs can read incident data, open incidents and spend Azure OpenAI tokens on RCA drafts. Reviewers still need to try the console with no setup beyond a username and password shared on request. How do we require a caller on every endpoint while keeping access simple for one audience?

## Decision drivers

- Nothing but health checks is anonymous: console, REST API, real-time hub, Insights, Swagger calls and Prometheus metrics.
- A reviewer signs in with one shared credential, without a Microsoft account or an invitation.
- Machine callers (Functions, Insights, Alertmanager, Azure Monitor, Prometheus) keep working with headers they can send; Azure Monitor webhooks cannot send custom headers.
- Authentication stays in the interface and infrastructure layers; domain and use cases do not change.
- Works the same in Azure and in the local Docker Compose stack, with no extra paid resource.
- The Static Web App stays on the Free plan.

## Considered options

1. Token endpoint in the API that issues an HS256 JWT for one configured demo account, plus the existing API key for services
2. Microsoft Entra ID (OIDC with MSAL in the console, JWT validation in the APIs)
3. Container Apps built-in authentication (Easy Auth)
4. Static Web Apps password protection

## Decision outcome

Chosen option: **a token endpoint that issues a signed JWT for one demo account, plus the API key for services**. `POST /api/auth/token` checks the configured username and password in constant time, behind its own rate limit, and returns a JWT (HS256, issuer `incident-ops-api`, audience `incident-ops`, 8 hours). The API registers a `Bearer` scheme, an `ApiKey` scheme and a policy scheme that picks one per request; a fallback authorization policy requires an authenticated caller on every endpoint that does not opt out. Service endpoints (escalate, alert ingestion, chaos, `/metrics`) accept only the API key. Insights validates the same JWT with the shared signing key. The hub reads the token from `access_token` because browsers cannot set headers on WebSocket requests.

### Consequences

- Good: every endpoint except health checks requires a caller, enforced by a fallback policy, so a new endpoint is protected by default.
- Good: one credential to share with reviewers; nothing to install or invite.
- Good: the same code path runs locally and in Azure, and is covered by integration tests.
- Good: domain, application and use cases are unchanged; the change sits in `IncidentOps.Api`, the Insights interface layer and the console.
- Bad: one shared account, so there is no per-person identity or audit; timeline actors stay operator-typed.
- Bad: tokens cannot be revoked one by one; rotating the signing key revokes all of them.
- Bad: the API now holds a password and a signing key, two more secrets to rotate.
- Bad: the token is readable by scripts on the console origin while it lives in `sessionStorage`.

## Pros and cons of the options

### Microsoft Entra ID

- Good: real identities, MFA, conditional access, revocation, no password handled by the API.
- Good: the standard choice for a production deployment of this system.
- Bad: reviewers need an account in the tenant or a B2B invitation, and an app registration with redirect URIs per environment; too much friction for a demo shared on request.
- Bad: the local stack needs a tenant or a mock identity provider.

### Container Apps built-in authentication (Easy Auth)

- Good: no code in the API; the platform rejects anonymous requests.
- Bad: it needs an identity provider (Entra ID, GitHub, Google), so reviewers still need an account and consent.
- Bad: it covers the container apps only; the Static Web App, the local stack and the tests do not exercise it.
- Bad: machine callers need exclusions or client credentials, and Azure Monitor webhooks cannot present them.

### Static Web Apps password protection

- Good: one shared password, no code.
- Bad: it requires the Standard plan.
- Bad: it protects only the static console; the APIs and the hub stay open to anyone who calls them directly.
