using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IncidentOps.Functions.Telemetry;

public static class TelemetryRegistration
{
    private const string ApplicationInsightsLoggerProvider =
        "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider";

    public static IServiceCollection AddIncidentOpsTelemetry(this IServiceCollection services)
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<ITelemetryInitializer, SignedUrlRedactor>();
        services.Configure<LoggerFilterOptions>(KeepInformationLogs);
        return services;
    }

    private static void KeepInformationLogs(LoggerFilterOptions options)
    {
        var defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName == ApplicationInsightsLoggerProvider);
        if (defaultRule is null)
        {
            return;
        }

        options.Rules.Remove(defaultRule);
    }
}
