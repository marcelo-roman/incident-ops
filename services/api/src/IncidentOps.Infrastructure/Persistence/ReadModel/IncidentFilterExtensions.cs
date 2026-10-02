using IncidentOps.Application.Incidents.ReadModel;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Infrastructure.Persistence.ReadModel;

internal static class IncidentFilterExtensions
{
    public static IQueryable<IncidentRecord> Matching(this IQueryable<IncidentRecord> query, IncidentFilter filter)
    {
        if (filter.Status is { } status)
        {
            query = query.Where(incident => incident.Status == status);
        }

        if (filter.Severity is { } severity)
        {
            query = query.Where(incident => incident.Severity == severity);
        }

        if (!string.IsNullOrWhiteSpace(filter.ServiceId))
        {
            query = query.Where(incident => incident.ServiceId == filter.ServiceId);
        }

        return filter.Open switch
        {
            true => query.Where(incident => incident.Status != IncidentStatus.Resolved),
            false => query.Where(incident => incident.Status == IncidentStatus.Resolved),
            null => query,
        };
    }
}
