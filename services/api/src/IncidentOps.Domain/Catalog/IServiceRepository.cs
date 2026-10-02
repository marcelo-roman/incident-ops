namespace IncidentOps.Domain.Catalog;

public interface IServiceRepository
{
    Task<bool> ExistsAsync(ServiceId serviceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceId>> ListIdsAsync(CancellationToken cancellationToken);
}
