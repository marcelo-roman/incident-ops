using System.Reflection;
using IncidentOps.Application.Incidents;
using IncidentOps.Domain.Incidents;
using IncidentOps.Infrastructure.Persistence;

namespace IncidentOps.Architecture.Tests;

internal static class Layers
{
    public const string Domain = "IncidentOps.Domain";
    public const string Application = "IncidentOps.Application";
    public const string Infrastructure = "IncidentOps.Infrastructure";
    public const string Api = "IncidentOps.Api";

    public static readonly Assembly DomainAssembly = typeof(Incident).Assembly;
    public static readonly Assembly ApplicationAssembly = typeof(IncidentView).Assembly;
    public static readonly Assembly InfrastructureAssembly = typeof(IncidentOpsDbContext).Assembly;
    public static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    public static readonly string[] Frameworks =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Microsoft.Data.SqlClient",
        "Azure",
    ];
}
