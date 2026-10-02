using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Catalog;

public sealed class Service : AggregateRoot<ServiceId>
{
    public Service(ServiceId id, string name, Tier tier, string ownerTeam)
        : base(id)
    {
        Name = Guard.Required(name, nameof(name), 128);
        Tier = Guard.Defined(tier, nameof(tier));
        OwnerTeam = Guard.Required(ownerTeam, nameof(ownerTeam), 128);
    }

    private Service()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public Tier Tier { get; private set; }

    public string OwnerTeam { get; private set; } = string.Empty;
}
