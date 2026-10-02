# Postmortem template

Blameless: we look for how the system, process and tooling allowed the failure, not who caused it. People acted reasonably on the information they had. Names appear only in the roles and action owners.

Copy into `docs/operations/postmortems/yyyy-mm-dd-<slug>.md`. Required for Sev1 and Sev2; published within 5 business days. Start from the AI draft (`POST /api/rca/draft` in Insights) if useful, then verify every line against the timeline and telemetry.

Example: [2026-09-17 payments-gateway timeouts](postmortems/2026-09-17-payments-gateway-timeouts.md).

---

```markdown
# INC-{number}: {title}

| | |
|---|---|
| Severity | Sev{n} |
| Service | {serviceId} |
| Date | {yyyy-mm-dd} |
| Duration | detection → mitigation {h:mm}; → resolution {h:mm} |
| SLA | ack {Met/Breached}, resolve {Met/Breached} |
| Incident commander | {name} |
| Postmortem owner | {name} |
| Status | Draft / In review / Published |

## Summary

{Three to five sentences: what failed, impact, how it was mitigated, root cause.}

## Impact

- Users/customers affected: {number or %}
- Requests failed or degraded: {number, %}
- Business impact: {orders, revenue, contractual}
- SLO error budget consumed: {%}

## Timeline (UTC)

| Time | Event |
|---|---|
| hh:mm | {change deployed / first symptom} |
| hh:mm | {alert fired / reported} |
| hh:mm | {acknowledged by} |
| hh:mm | {key finding} |
| hh:mm | {mitigation applied} |
| hh:mm | {impact ended} |
| hh:mm | {resolved} |

Time to detect: {m}. Time to acknowledge: {m}. Time to mitigate: {m}.

## Root cause

{The underlying cause, technical and organizational. Use the five whys if it helps; stop at something the team can change.}

## Contributing factors

- {Factor: missing test, alert gap, unclear runbook, config drift, capacity, process}

## Detection

{How we found out. Would an alert have caught it earlier? Did the customer find it first?}

## Response

What went well:
- {}

What was hard:
- {}

Where we got lucky:
- {}

## Action items

| # | Action | Type | Owner | Due | Work item |
|---|---|---|---|---|---|
| 1 | {} | prevent / detect / mitigate / process | {} | {} | AB#{} |

Types: **prevent** stops recurrence; **detect** finds it faster; **mitigate** reduces impact or time to restore; **process** changes how we work.

## Lessons

{What the rest of engineering should know. One paragraph.}
```

## Review meeting

- 45 minutes, within 5 business days, open invitation.
- Facilitator is not the IC.
- Walk the timeline, agree on root cause and contributing factors, agree action items with owners and dates.
- Action items go to Azure Boards tagged `incident-followup` and tracked in [retrospective actions](../leadership/retrospective-actions.md).
- Ask "what made this the reasonable thing to do at the time?" instead of "why did you do that?".
