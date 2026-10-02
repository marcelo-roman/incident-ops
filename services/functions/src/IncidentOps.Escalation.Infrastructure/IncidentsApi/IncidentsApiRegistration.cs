using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Infrastructure.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IncidentOps.Escalation.Infrastructure.IncidentsApi;

public static class IncidentsApiRegistration
{
    public const string ApiKeyHeader = "X-Api-Key";

    public static IServiceCollection AddIncidentsApi(this IServiceCollection services)
    {
        services.AddOptions<IncidentsApiOptions>()
            .BindConfiguration(IncidentsApiOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IIncidentReader, HttpIncidentReader>(UseApi).AddEscalationResilience();
        services.AddHttpClient<IOnCallDirectory, HttpOnCallDirectory>(UseApi).AddEscalationResilience();
        services.AddHttpClient<IIncidentEscalator, HttpIncidentEscalator>(UseApiWithKey).AddEscalationResilience();
        return services;
    }

    private static void UseApi(IServiceProvider provider, HttpClient client)
    {
        client.BaseAddress = provider.GetRequiredService<IOptions<IncidentsApiOptions>>().Value.BaseUrl;
    }

    private static void UseApiWithKey(IServiceProvider provider, HttpClient client)
    {
        UseApi(provider, client);
        client.DefaultRequestHeaders.Add(ApiKeyHeader, provider.GetRequiredService<IOptions<IncidentsApiOptions>>().Value.ApiKey);
    }
}
