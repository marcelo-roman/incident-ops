using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Incidents;
using Microsoft.EntityFrameworkCore;

namespace IncidentOps.Infrastructure.Persistence.Repositories;

internal sealed class IncidentRepository(IncidentOpsDbContext db) : IIncidentRepository
{
    public Task<Incident?> FindAsync(IncidentId id, CancellationToken cancellationToken) =>
        db.Incidents
            .Include(incident => incident.Timeline)
            .FirstOrDefaultAsync(incident => incident.Id == id, cancellationToken);

    public Task<Incident?> FindOpenByFingerprintAsync(AlertFingerprint fingerprint, CancellationToken cancellationToken) =>
        db.Incidents
            .Include(incident => incident.Timeline)
            .FirstOrDefaultAsync(
                incident => incident.AlertFingerprint == fingerprint && incident.Status != IncidentStatus.Resolved,
                cancellationToken);

    public async Task<IncidentNumber> NextNumberAsync(CancellationToken cancellationToken)
    {
        var numbers = await db.Database
            .SqlQuery<int>($"SELECT NEXT VALUE FOR [dbo].[IncidentNumbers] AS [Value]")
            .ToListAsync(cancellationToken);

        return new IncidentNumber(numbers[0]);
    }

    public void Add(Incident incident) => db.Incidents.Add(incident);
}
