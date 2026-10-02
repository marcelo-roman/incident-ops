using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents;

public sealed record TimelineEntryView(
    Guid Id,
    Guid IncidentId,
    DateTimeOffset At,
    TimelineKind Kind,
    string Actor,
    string Message);
