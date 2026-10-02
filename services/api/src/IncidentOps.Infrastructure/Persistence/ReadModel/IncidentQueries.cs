using IncidentOps.Application.Incidents.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace IncidentOps.Infrastructure.Persistence.ReadModel;

internal sealed class IncidentQueries(IncidentOpsReadDbContext db) : IIncidentQueries
{
    public Task<IncidentRecord?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Incidents.FirstOrDefaultAsync(incident => incident.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TimelineEntryRecord>> TimelineAsync(Guid incidentId, CancellationToken cancellationToken) =>
        await db.TimelineEntries
            .Where(entry => entry.IncidentId == incidentId)
            .OrderBy(entry => entry.Sequence)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<IncidentRecord>> ListAsync(IncidentFilter filter, CancellationToken cancellationToken) =>
        await db.Incidents
            .Matching(filter)
            .OrderByDescending(incident => incident.CreatedAt)
            .Take(filter.Limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<IncidentRecord>> ListCreatedBetweenAsync(
        DateTimeOffset createdFrom,
        DateTimeOffset createdUntil,
        CancellationToken cancellationToken) =>
        await db.Incidents
            .Where(incident => incident.CreatedAt >= createdFrom && incident.CreatedAt <= createdUntil)
            .OrderBy(incident => incident.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<IncidentRecord>> ListOpenOrCreatedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken) =>
        await db.Incidents
            .Where(incident => incident.Status != Domain.Incidents.IncidentStatus.Resolved || incident.CreatedAt >= since)
            .ToListAsync(cancellationToken);
}
