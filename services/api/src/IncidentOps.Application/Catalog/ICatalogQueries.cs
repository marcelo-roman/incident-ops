namespace IncidentOps.Application.Catalog;

public interface ICatalogQueries
{
    Task<IReadOnlyList<ServiceView>> ListServicesAsync(CancellationToken cancellationToken);
}
