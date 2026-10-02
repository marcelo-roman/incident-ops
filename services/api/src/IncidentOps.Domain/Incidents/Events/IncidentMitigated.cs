namespace IncidentOps.Domain.Incidents.Events;

public sealed record IncidentMitigated(IncidentId IncidentId, Guid TimelineEntryId, DateTimeOffset OccurredAt)
    : IncidentDomainEvent(IncidentId, TimelineEntryId, OccurredAt);
