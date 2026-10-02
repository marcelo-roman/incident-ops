# Severity and SLA

Severity describes impact, not effort or urgency of a fix. When in doubt, pick the higher severity; it can be lowered.

## SLA policy

Source of truth: [contracts.md](https://github.com/marcelo-roman/incident-ops/blob/main/contracts/contracts.md#sla-policy). The API computes deadlines from this table.

| Severity | Acknowledge within | Resolve within | Pages | Escalation |
| --- | --- | --- | --- | --- |
| `Sev1` | 15 minutes | 4 hours | yes, 24×7 | primary → secondary → lead, every 15 min unacknowledged |
| `Sev2` | 30 minutes | 8 hours | yes, 24×7 | every 30 min unacknowledged |
| `Sev3` | 4 hours | 3 days | no | every 4 h unacknowledged |
| `Sev4` | 1 business day (24h) | 10 days | no | every 24 h unacknowledged |

"Resolve" means the incident is closed with a root cause. Service restored early is `Mitigated`; the resolve clock keeps running until `Resolved`, which keeps permanent fixes from drifting.

## Severity definitions

| Severity | Impact | Typical scope |
| --- | --- | --- |
| `Sev1` | Critical business function unavailable or data at risk; no workaround | Tier1 service down or failing for > 25% of requests; data loss or exposure; security breach |
| `Sev2` | Major function degraded or unavailable for a subset of users; workaround painful | Tier1 service degraded; Tier2 service down; SLO burn rate > 10× |
| `Sev3` | Minor function impaired; workaround exists | Tier2 degraded; Tier3 down; single customer affected |
| `Sev4` | Cosmetic or low-impact; no user-facing urgency | Tier3 degraded; internal tooling issue; latent risk discovered |

## Examples

Using the seeded services.

| Severity | Example |
| --- | --- |
| `Sev1` | `payments-gateway` timing out on 40% of authorizations; checkout cannot complete orders |
| `Sev1` | `identity` token endpoint returning 500; nobody can sign in |
| `Sev1` | Personal data visible to the wrong customer in `reporting` exports |
| `Sev2` | `checkout` p95 latency at 6 s (SLO 800 ms); orders complete but abandonment rises |
| `Sev2` | `notifications` not sending order confirmation emails; orders unaffected |
| `Sev2` | `search` down; customers can still browse categories |
| `Sev3` | `search` returning stale results (index 3 h behind) |
| `Sev3` | `reporting` nightly export failed; data available on retry |
| `Sev3` | One merchant's webhooks failing due to their certificate change |
| `Sev4` | Wrong currency symbol on an internal `reporting` dashboard |
| `Sev4` | TLS certificate for an internal endpoint expires in 20 days |

## SLA state

As defined in the contract and shown on every incident:

| `slaState` | Condition |
| --- | --- |
| `OnTrack` | open deadline with ≥ 25% of the active window remaining |
| `AtRisk` | < 25% of the active window remaining |
| `Breached` | an open deadline passed (ack while `Triggered`, resolve while not `Resolved`) |
| `Met` | resolved within `resolveDueAt` |

The active window is the acknowledgement window while `Triggered` and the resolve window afterwards. `AtRisk` incidents are listed first on the console and called out in the daily standup.

## Service tiers

Seeded services and owning teams come from `GET /api/services`. `platform` is the Incident Ops platform itself and the fallback for alerts without a known service.

| Tier | Services | Default severity floor for a full outage |
| --- | --- | --- |
| `Tier1` | `checkout`, `payments-gateway`, `identity`, `platform` | `Sev1` |
| `Tier2` | `notifications`, `search` | `Sev2` |
| `Tier3` | `reporting` | `Sev3` |

## Measuring compliance

- **SLA compliance** = resolved incidents with `resolvedAt ≤ resolveDueAt` ÷ resolved incidents, per period. Reported by `GET /api/metrics/summary` (30 days) and Insights `/api/kpis` (per service and severity).
- **Ack compliance** = incidents acknowledged by their original `ackDueAt` ÷ incidents acknowledged.
- Targets: Sev1/Sev2 resolve compliance ≥ 95%, Sev3/Sev4 ≥ 90%, ack compliance ≥ 98%. See [KTLO metrics](ktlo-metrics.md).
