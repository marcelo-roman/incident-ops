using IncidentOps.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace IncidentOps.Infrastructure.Persistence.Repositories;

internal sealed class ServiceRepository(IncidentOpsDbContext db) : IServiceRepository
{
    public Task<bool> ExistsAsync(ServiceId serviceId, CancellationToken cancellationToken) =>
        db.Services.AnyAsync(service => service.Id == serviceId, cancellationToken);

    public async Task<IReadOnlyList<ServiceId>> ListIdsAsync(CancellationToken cancellationToken) =>
        await db.Services.Select(service => service.Id).ToListAsync(cancellationToken);
}
