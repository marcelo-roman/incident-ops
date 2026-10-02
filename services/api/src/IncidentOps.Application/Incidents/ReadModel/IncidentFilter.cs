using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents.ReadModel;

public sealed record IncidentFilter(
    IncidentStatus? Status,
    Severity? Severity,
    string? ServiceId,
    bool? Open,
    int Limit);
