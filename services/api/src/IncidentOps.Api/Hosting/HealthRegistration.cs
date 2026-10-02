using IncidentOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace IncidentOps.Api.Hosting;

internal static class HealthRegistration
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddIncidentOpsHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddDbContextCheck<IncidentOpsDbContext>("database", tags: [ReadyTag]);
        return services;
    }

    public static WebApplication MapIncidentOpsHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });
        return app;
    }
}
