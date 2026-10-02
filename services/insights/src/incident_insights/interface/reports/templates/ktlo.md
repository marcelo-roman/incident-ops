# KTLO report: {{ report.window.start.date() }} to {{ report.window.end.date() }}

Generated {{ report.generated_at.strftime("%Y-%m-%d %H:%M UTC") }} over the trailing {{ report.window.days }} days.

## Headline

| Incidents | Detected by monitoring | SLA compliance | Acknowledge SLA | Resolve SLA | MTTA median | MTTA p90 | MTTR median | MTTR p90 |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| {{ report.overall.incidents }} | {{ report.detection.alerting_pct | percent }} | {{ report.overall.sla.overall_pct | percent }} | {{ report.overall.sla.acknowledge_pct | percent }} | {{ report.overall.sla.resolve_pct | percent }} | {{ report.overall.mtta.median_minutes | minutes }} | {{ report.overall.mtta.p90_minutes | minutes }} | {{ report.overall.mttr.median_minutes | minutes }} | {{ report.overall.mttr.p90_minutes | minutes }} |

## Detection

| Source | Incidents | Share | MTTA median | MTTA p90 |
|---|---:|---:|---:|---:|
{% for item in report.detection.by_source %}
| {{ item.source }} | {{ item.incidents }} | {{ item.share_pct | percent }} | {{ item.mtta.median_minutes | minutes }} | {{ item.mtta.p90_minutes | minutes }} |
{% endfor %}

## Top recurring issues

{% if report.top_recurring %}
| Cluster | Incidents | Services | Example |
|---|---:|---|---|
{% for cluster in report.top_recurring %}
| {{ cluster.label | cell }} | {{ cluster.count }} | {% for service in cluster.services %}{{ service.service_id }} ({{ service.count }}){% if not loop.last %}, {% endif %}{% endfor %} | {{ cluster.sample_titles[0] | cell }} |
{% endfor %}
{% else %}
No recurring clusters in this window.
{% endif %}

## SLA breaches

{% if report.breaches %}
{{ report.total_breaches }} incidents breached at least one SLA target{% if report.total_breaches > report.breaches | length %}; the {{ report.breaches | length }} most severe are listed{% endif %}.

| Incident | Severity | Service | Status | Breached | Resolve overrun | Assignee |
|---|---|---|---|---|---:|---|
{% for breach in report.breaches %}
| INC-{{ breach.number }} {{ breach.title | cell }} | {{ breach.severity }} | {{ breach.service_id }} | {{ breach.status }} | {{ breach.breached_targets | join(", ") }} | {{ breach.resolve_overrun_minutes | minutes }} | {{ breach.assignee or "unassigned" }} |
{% endfor %}
{% else %}
No SLA breaches in this window.
{% endif %}

## MTTR by service

| Service | Incidents | MTTR median | MTTR p90 | MTTA median | SLA compliance |
|---|---:|---:|---:|---:|---:|
{% for group in report.mttr_by_service %}
| {{ group.service_id }} | {{ group.incidents }} | {{ group.mttr.median_minutes | minutes }} | {{ group.mttr.p90_minutes | minutes }} | {{ group.mtta.median_minutes | minutes }} | {{ group.sla.overall_pct | percent }} |
{% endfor %}

## On-call load

Off-hours means created outside 09:00-18:00 America/New_York on weekdays.

| Assignee | Incidents | Sev1/Sev2 | Off-hours | Escalated |
|---|---:|---:|---:|---:|
{% for load in report.on_call %}
| {{ load.assignee }} | {{ load.incidents }} | {{ load.high_severity }} | {{ load.off_hours }} | {{ load.escalated }} |
{% endfor %}
