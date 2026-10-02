from incident_insights.domain.incidents.incident import IncidentRecord
from incident_insights.domain.incidents.status import IncidentStatus
from incident_insights.domain.rca.draft import ActionItem, Priority, RcaDraft, RcaTimelineEvent
from incident_insights.domain.shared.duration_text import describe_duration
from incident_insights.domain.shared.severity import Severity

IMPACT_BY_SEVERITY = {
    Severity.SEV1: "Critical outage or data risk",
    Severity.SEV2: "Major degradation of a core capability",
    Severity.SEV3: "Partial degradation with a workaround",
    Severity.SEV4: "Minor issue with limited user impact",
}

NO_FACTORS = (
    "The incident record does not show SLA overruns or a recorded root cause; "
    "confirm contributing factors with the responders."
)

ON_CALL_OWNER = "On-call engineering lead"


class RcaDraftComposer:
    def compose(self, incident: IncidentRecord) -> RcaDraft:
        return RcaDraft(
            summary=self._summary(incident),
            impact=self._impact(incident),
            timeline=self._timeline(incident),
            contributing_factors=self._contributing_factors(incident),
            action_items=self._action_items(incident),
        )

    def _summary(self, incident: IncidentRecord) -> str:
        opening = (
            f"INC-{incident.number} ({incident.severity.value}) affected {incident.service}: "
            f"{incident.title}."
        )
        resolution = incident.time_to_resolve()
        if resolution is None:
            return (
                f"{opening} The incident is {incident.status.value.lower()} and not yet resolved."
            )
        return f"{opening} It was resolved after {describe_duration(resolution)}."

    def _impact(self, incident: IncidentRecord) -> str:
        scope = f"{IMPACT_BY_SEVERITY[incident.severity]} on {incident.service}."
        impact_window = incident.time_to_mitigate() or incident.time_to_resolve()
        sla = self._sla_sentence(incident)
        if impact_window is None or incident.status is IncidentStatus.TRIGGERED:
            return f"{scope} Impact is ongoing. {sla}"
        return f"{scope} Impact lasted about {describe_duration(impact_window)}. {sla}"

    def _sla_sentence(self, incident: IncidentRecord) -> str:
        if not incident.is_resolved:
            return f"Live SLA state: {incident.sla_state.value}."
        if incident.complies_with_sla():
            return "The incident met its SLA."
        missed = [
            target
            for target, breached in (
                (
                    "acknowledgement",
                    incident.acknowledgement_breached or incident.acknowledged_at is None,
                ),
                ("resolution", incident.resolved_later_than_policy()),
            )
            if breached
        ]
        return f"The incident breached its SLA ({', '.join(missed)})."

    def _timeline(self, incident: IncidentRecord) -> tuple[RcaTimelineEvent, ...]:
        return tuple(
            RcaTimelineEvent(
                at=entry.at.isoformat().replace("+00:00", "Z"),
                event=f"{entry.kind.value}: {entry.message} ({entry.actor})",
            )
            for entry in incident.chronology()
        )

    def _contributing_factors(self, incident: IncidentRecord) -> tuple[str, ...]:
        factors = [
            factor
            for factor in (
                self._root_cause(incident),
                self._manual_detection(incident),
                self._late_acknowledgement(incident),
                self._escalation(incident),
                self._late_resolution(incident),
            )
            if factor is not None
        ]
        return tuple(factors or [NO_FACTORS])

    def _root_cause(self, incident: IncidentRecord) -> str | None:
        if incident.root_cause is None:
            return None
        return f"Recorded root cause: {incident.root_cause}"

    def _manual_detection(self, incident: IncidentRecord) -> str | None:
        if incident.detected_by_monitoring():
            return None
        return "No alert fired; the incident was opened manually."

    def _late_acknowledgement(self, incident: IncidentRecord) -> str | None:
        if not incident.acknowledgement_breached:
            return None
        target = describe_duration(incident.targets.acknowledge_within)
        elapsed = incident.time_to_acknowledge()
        if elapsed is None:
            return f"Acknowledgement SLA breached: no acknowledgement within the {target} target."
        return (
            f"Acknowledgement SLA breached: acknowledged after {describe_duration(elapsed)} "
            f"against the {target} target for {incident.severity.value}."
        )

    def _escalation(self, incident: IncidentRecord) -> str | None:
        if not incident.is_escalated:
            return None
        return (
            f"The page escalated to level {incident.escalation_level} before it was acknowledged."
        )

    def _late_resolution(self, incident: IncidentRecord) -> str | None:
        elapsed = incident.time_to_resolve()
        if elapsed is None or not incident.resolved_later_than_policy():
            return None
        return (
            f"Resolution took {describe_duration(elapsed)}, beyond the "
            f"{describe_duration(incident.targets.resolve_within)} target."
        )

    def _action_items(self, incident: IncidentRecord) -> tuple[ActionItem, ...]:
        owner = f"{incident.service} owners"
        priority = Priority.for_severity(incident.severity)
        items = [
            ActionItem(
                f"Confirm and document the root cause of INC-{incident.number}", owner, priority
            )
        ]
        if not incident.detected_by_monitoring():
            items.append(
                ActionItem(
                    f"Add an alert that detects '{incident.title}' before customers do",
                    owner,
                    priority,
                )
            )
        if incident.acknowledgement_breached:
            items.append(
                ActionItem(
                    f"Review paging and acknowledgement for {incident.service} on-call",
                    ON_CALL_OWNER,
                    priority,
                )
            )
        if incident.resolved_later_than_policy():
            items.append(
                ActionItem(
                    f"Write or update the runbook for '{incident.title}'", owner, Priority.P2
                )
            )
        return tuple(items)
