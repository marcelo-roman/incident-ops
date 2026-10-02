using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents.Messaging;

namespace IncidentOps.Api.RealTime;

internal static class RealTimeRegistration
{
    public const string AzureSignalRConnectionString = "Azure:SignalR:ConnectionString";

    public static IServiceCollection AddRealTime(this IServiceCollection services, IConfiguration configuration)
    {
        var signalR = services
            .AddSignalR()
            .AddJsonProtocol(options => ContractJson.Configure(options.PayloadSerializerOptions));

        var connectionString = configuration[AzureSignalRConnectionString];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            signalR.AddAzureSignalR(options => options.ConnectionString = connectionString);
        }

        return services.AddSingleton<IIncidentNotifier, SignalRIncidentNotifier>();
    }
}
