using IncidentOps.Api.Alerts;
using IncidentOps.Api.Authentication;
using IncidentOps.Api.Catalog;
using IncidentOps.Api.Chaos;
using IncidentOps.Api.Errors;
using IncidentOps.Api.Incidents;
using IncidentOps.Api.Metrics;
using IncidentOps.Api.OnCall;
using IncidentOps.Api.RealTime;
using IncidentOps.Api.Security;
using IncidentOps.Infrastructure.Seeding;

namespace IncidentOps.Api.Hosting;

internal static class ApiPipeline
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync(app.Lifetime.ApplicationStopping);
    }

    public static WebApplication UseIncidentOpsPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseCors();
        app.UseRateLimiter();
        if (app.Configuration.IsChaosEnabled())
        {
            app.UseMiddleware<ChaosMiddleware>();
        }

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static WebApplication MapIncidentOpsEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").AddEndpointFilter<ProblemEndpointFilter>();
        api.MapTokenEndpoints();
        api.MapServiceEndpoints();
        api.MapOnCallEndpoints();
        api.MapMetricsEndpoints();
        api.MapAlertEndpoints();
        api.MapGroup("/incidents")
            .WithTags("Incidents")
            .MapIncidentQueryEndpoints()
            .MapIncidentCommandEndpoints();

        if (app.Configuration.IsChaosEnabled())
        {
            api.MapChaosEndpoints();
        }

        app.MapHub<IncidentsHub>(IncidentsHub.Path).RequireAuthorization();
        app.MapPrometheusScrapingEndpoint("/metrics").RequireApiKey(ApiKeySources.Header | ApiKeySources.AuthorizationHeader);
        app.MapIncidentOpsHealthChecks();
        return app;
    }
}
