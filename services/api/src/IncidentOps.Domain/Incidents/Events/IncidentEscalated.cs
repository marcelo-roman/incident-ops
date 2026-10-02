namespace IncidentOps.Domain.Incidents.Events;

public sealed record IncidentEscalated(IncidentId IncidentId, Guid TimelineEntryId, DateTimeOffset OccurredAt, EscalationLevel Level)
    : IncidentDomainEvent(IncidentId, TimelineEntryId, OccurredAt);
