namespace IncidentOps.Domain.Incidents.Events;

public sealed record IncidentAcknowledged(IncidentId IncidentId, Guid TimelineEntryId, DateTimeOffset OccurredAt)
    : IncidentDomainEvent(IncidentId, TimelineEntryId, OccurredAt);
