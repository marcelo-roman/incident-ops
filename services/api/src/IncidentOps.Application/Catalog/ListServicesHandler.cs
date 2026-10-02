namespace IncidentOps.Application.Catalog;

public sealed class ListServicesHandler(ICatalogQueries queries)
{
    public Task<IReadOnlyList<ServiceView>> HandleAsync(CancellationToken cancellationToken) =>
        queries.ListServicesAsync(cancellationToken);
}
