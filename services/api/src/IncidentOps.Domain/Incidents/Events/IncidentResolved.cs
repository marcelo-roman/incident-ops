namespace IncidentOps.Domain.Incidents.Events;

public sealed record IncidentResolved(IncidentId IncidentId, Guid TimelineEntryId, DateTimeOffset OccurredAt)
    : IncidentDomainEvent(IncidentId, TimelineEntryId, OccurredAt);
