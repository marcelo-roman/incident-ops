using IncidentOps.Application.Catalog;
using Microsoft.EntityFrameworkCore;

namespace IncidentOps.Infrastructure.Persistence.ReadModel;

internal sealed class CatalogQueries(IncidentOpsReadDbContext db) : ICatalogQueries
{
    public async Task<IReadOnlyList<ServiceView>> ListServicesAsync(CancellationToken cancellationToken) =>
        await db.Services
            .OrderBy(service => service.Tier)
            .ThenBy(service => service.Id)
            .Select(service => new ServiceView(service.Id, service.Name, service.Tier, service.OwnerTeam))
            .ToListAsync(cancellationToken);
}
