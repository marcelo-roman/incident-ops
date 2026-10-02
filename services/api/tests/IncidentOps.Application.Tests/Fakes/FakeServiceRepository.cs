using IncidentOps.Application.Catalog;
using IncidentOps.Domain.Catalog;

namespace IncidentOps.Application.Tests.Fakes;

public sealed class FakeServiceRepository : IServiceRepository, ICatalogQueries
{
    private static readonly IReadOnlyList<Service> Services =
    [
        new(new ServiceId("checkout"), "Checkout", Tier.Tier1, "Commerce"),
        new(new ServiceId("identity"), "Identity", Tier.Tier1, "Identity and Access"),
        new(ServiceId.Platform, "Platform", Tier.Tier1, "Platform Engineering"),
    ];

    public Task<bool> ExistsAsync(ServiceId serviceId, CancellationToken cancellationToken) =>
        Task.FromResult(Services.Any(service => service.Id == serviceId));

    public Task<IReadOnlyList<ServiceId>> ListIdsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ServiceId> ids = Services.Select(service => service.Id).ToList();
        return Task.FromResult(ids);
    }

    public Task<IReadOnlyList<ServiceView>> ListServicesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ServiceView> views = Services
            .Select(service => new ServiceView(service.Id.Value, service.Name, service.Tier, service.OwnerTeam))
            .ToList();
        return Task.FromResult(views);
    }
}
