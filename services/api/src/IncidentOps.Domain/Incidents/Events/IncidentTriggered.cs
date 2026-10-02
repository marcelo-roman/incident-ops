namespace IncidentOps.Domain.Incidents.Events;

public sealed record IncidentTriggered(IncidentId IncidentId, Guid TimelineEntryId, DateTimeOffset OccurredAt)
    : IncidentDomainEvent(IncidentId, TimelineEntryId, OccurredAt);
