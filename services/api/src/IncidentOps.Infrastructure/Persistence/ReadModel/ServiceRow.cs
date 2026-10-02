using IncidentOps.Domain.Catalog;

namespace IncidentOps.Infrastructure.Persistence.ReadModel;

internal sealed class ServiceRow
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public Tier Tier { get; init; }

    public string OwnerTeam { get; init; } = string.Empty;
}
