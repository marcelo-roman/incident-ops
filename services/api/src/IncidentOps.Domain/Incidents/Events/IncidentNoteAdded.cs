namespace IncidentOps.Domain.Incidents.Events;

public sealed record IncidentNoteAdded(IncidentId IncidentId, Guid TimelineEntryId, DateTimeOffset OccurredAt)
    : IncidentDomainEvent(IncidentId, TimelineEntryId, OccurredAt);
