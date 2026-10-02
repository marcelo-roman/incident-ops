using IncidentOps.Domain.Incidents.Events;

namespace IncidentOps.Application.Incidents.Messaging;

public static class IncidentEventTypes
{
    public const string Triggered = "incident.triggered";
    public const string Acknowledged = "incident.acknowledged";
    public const string Escalated = "incident.escalated";
    public const string Mitigated = "incident.mitigated";
    public const string Resolved = "incident.resolved";

    public static string? For(IncidentDomainEvent domainEvent) => domainEvent switch
    {
        IncidentTriggered => Triggered,
        IncidentAcknowledged => Acknowledged,
        IncidentEscalated => Escalated,
        IncidentMitigated => Mitigated,
        IncidentResolved => Resolved,
        _ => null,
    };
}
