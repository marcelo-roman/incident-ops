using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.OnCall;

namespace IncidentOps.Infrastructure.Seeding;

public static class ReferenceData
{
    public static IReadOnlyList<Service> Services() =>
    [
        new(new ServiceId("checkout"), "Checkout", Tier.Tier1, "Commerce"),
        new(new ServiceId("payments-gateway"), "Payments Gateway", Tier.Tier1, "Payments"),
        new(new ServiceId("identity"), "Identity", Tier.Tier1, "Identity and Access"),
        new(ServiceId.Platform, "Platform", Tier.Tier1, "Platform Engineering"),
        new(new ServiceId("notifications"), "Notifications", Tier.Tier2, "Messaging"),
        new(new ServiceId("search"), "Search", Tier.Tier2, "Discovery"),
        new(new ServiceId("reporting"), "Reporting", Tier.Tier3, "Data Platform"),
    ];

    public static IReadOnlyList<Engineer> Engineers() =>
    [
        new("ava.thompson", "Ava Thompson", EngineerRole.Rotation, 0),
        new("diego.martins", "Diego Martins", EngineerRole.Rotation, 1),
        new("priya.raman", "Priya Raman", EngineerRole.Rotation, 2),
        new("lukas.becker", "Lukas Becker", EngineerRole.Rotation, 3),
        new("mei.tanaka", "Mei Tanaka", EngineerRole.Rotation, 4),
        new("samuel.okafor", "Samuel Okafor", EngineerRole.Rotation, 5),
        new("rachel.kim", "Rachel Kim", EngineerRole.Lead, 0),
    ];
}
