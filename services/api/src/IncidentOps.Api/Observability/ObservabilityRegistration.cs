using Azure.Monitor.OpenTelemetry.AspNetCore;
using IncidentOps.Application.Incidents.Messaging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace IncidentOps.Api.Observability;

internal static class ObservabilityRegistration
{
    public const string ServiceName = "incident-ops-api";
    public const string ApplicationInsightsConnectionString = "APPLICATIONINSIGHTS_CONNECTION_STRING";
    public const string ServiceNameVariable = "OTEL_SERVICE_NAME";

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Logging.Configure(options => options.ActivityTrackingOptions =
            ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId);

        var serviceName = builder.Configuration[ServiceNameVariable] ?? ServiceName;
        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddMeter(IncidentOpsMetrics.MeterName)
                .AddPrometheusExporter());

        if (!string.IsNullOrWhiteSpace(builder.Configuration[ApplicationInsightsConnectionString]))
        {
            openTelemetry.UseAzureMonitor();
        }

        builder.Services.Configure<ObservabilityOptions>(builder.Configuration.GetSection(ObservabilityOptions.SectionName));
        builder.Services.AddSingleton<IncidentOpsMetrics>();
        builder.Services.AddSingleton<IIncidentNotifier, MetricsIncidentNotifier>();
        builder.Services.AddHostedService<IncidentGaugeRefresher>();
        return builder;
    }
}
