using IncidentOps.Domain.Catalog;

namespace IncidentOps.Application.Catalog;

public sealed record ServiceView(string Id, string Name, Tier Tier, string OwnerTeam);
