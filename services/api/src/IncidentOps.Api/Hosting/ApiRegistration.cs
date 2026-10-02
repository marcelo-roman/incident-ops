using IncidentOps.Api.Chaos;
using IncidentOps.Api.Demo;
using IncidentOps.Api.Errors;
using IncidentOps.Api.Observability;
using IncidentOps.Api.RealTime;
using IncidentOps.Api.Security;
using IncidentOps.Application;
using IncidentOps.Application.Common;
using IncidentOps.Application.Sla;
using IncidentOps.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

namespace IncidentOps.Api.Hosting;

internal static class ApiRegistration
{
    public static WebApplicationBuilder AddIncidentOpsApi(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddOptions<SlaOptions>()
            .Bind(configuration.GetSection(SlaOptions.SectionName))
            .Validate(options => options.TimeScale > 0, "Sla:TimeScale must be greater than zero.")
            .ValidateOnStart();
        services.AddIncidentOpsSecurity(configuration);
        services.ConfigureHttpJsonOptions(options => ContractJson.Configure(options.SerializerOptions));
        services.Configure<ForwardedHeadersOptions>(ConfigureForwardedHeaders);
        services.AddProblemDetails();
        services.AddSingleton<ProblemEndpointFilter>();
        services.AddRealTime(configuration);
        services.AddClientRateLimiting(configuration);
        services.AddConsoleCors(configuration);
        services.AddIncidentOpsHealthChecks();
        services.AddIncidentOpsOpenApi();
        services.AddDemoTraffic(configuration);
        services.AddChaos();
        builder.AddObservability();
        return builder;
    }

    private static void ConfigureForwardedHeaders(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
}
