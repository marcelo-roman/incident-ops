using IncidentOps.Application.Incidents.ReadModel;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents.Listing;

public sealed record ListIncidents(
    IncidentStatus? Status,
    Severity? Severity,
    string? ServiceId,
    bool? Open,
    int? Limit)
{
    public const int DefaultLimit = 100;
    public const int MaxLimit = 500;

    public IncidentFilter ToFilter() =>
        new(Status, Severity, ServiceId, Open, Math.Clamp(Limit ?? DefaultLimit, 1, MaxLimit));
}
