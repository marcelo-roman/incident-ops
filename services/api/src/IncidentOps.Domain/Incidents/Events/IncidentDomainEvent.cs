using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Incidents.Events;

public abstract record IncidentDomainEvent(IncidentId IncidentId, Guid TimelineEntryId, DateTimeOffset OccurredAt) : IDomainEvent;
