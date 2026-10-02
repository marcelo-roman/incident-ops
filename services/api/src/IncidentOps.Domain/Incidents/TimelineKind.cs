namespace IncidentOps.Domain.Incidents;

public enum TimelineKind
{
    Triggered,
    Acknowledged,
    Escalated,
    Mitigated,
    Resolved,
    Note,
    Alert,
}
