using IncidentOps.Domain.Alerts;

namespace IncidentOps.Domain.Incidents.Events;

public sealed record AlertRecorded(IncidentId IncidentId, Guid TimelineEntryId, DateTimeOffset OccurredAt, AlertFingerprint Fingerprint, AlertStatus Status)
    : IncidentDomainEvent(IncidentId, TimelineEntryId, OccurredAt);
