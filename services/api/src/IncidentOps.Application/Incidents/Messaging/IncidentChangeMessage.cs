namespace IncidentOps.Application.Incidents.Messaging;

public sealed record IncidentChangeMessage(
    Guid Id,
    string? EventType,
    DateTimeOffset OccurredAt,
    IncidentView Incident,
    TimelineEntryView Entry)
{
    public const string MessageType = "incident-change";
}
