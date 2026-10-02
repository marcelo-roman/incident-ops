using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents;

public static class IncidentRepositoryExtensions
{
    public static async Task<Incident> GetRequiredAsync(this IIncidentRepository incidents, Guid id, CancellationToken cancellationToken) =>
        await incidents.FindAsync(new IncidentId(id), cancellationToken) ?? throw new IncidentNotFoundException(id);
}
