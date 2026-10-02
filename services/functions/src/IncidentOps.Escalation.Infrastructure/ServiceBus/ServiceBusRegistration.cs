using Azure.Identity;
using Azure.Messaging.ServiceBus;
using IncidentOps.Escalation.Application.Ports;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentOps.Escalation.Infrastructure.ServiceBus;

public static class ServiceBusRegistration
{
    public static IServiceCollection AddSlaCheckScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetSection(ServiceBusSettings.Connection);
        services.AddAzureClients(clients => AddClient(clients, connection));
        var slaChecksQueue = Required(configuration, ServiceBusSettings.SlaChecksQueue);
        services.AddSingleton(provider => provider.GetRequiredService<ServiceBusClient>().CreateSender(slaChecksQueue));
        services.AddSingleton<ISlaCheckScheduler, ServiceBusSlaCheckScheduler>();
        return services;
    }

    private static void AddClient(AzureClientFactoryBuilder clients, IConfigurationSection connection)
    {
        var fullyQualifiedNamespace = connection["fullyQualifiedNamespace"];
        if (!string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
        {
            clients.AddServiceBusClientWithNamespace(fullyQualifiedNamespace).WithCredential(Credential(connection["clientId"]));
            return;
        }

        if (string.IsNullOrWhiteSpace(connection.Value))
        {
            throw new InvalidOperationException(
                $"Configure '{ServiceBusSettings.Connection}__fullyQualifiedNamespace' or the '{ServiceBusSettings.Connection}' connection string.");
        }

        clients.AddServiceBusClient(connection.Value);
    }

    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configure '{key}'.");
        }

        return value;
    }

    private static DefaultAzureCredential Credential(string? managedIdentityClientId) =>
        new(new DefaultAzureCredentialOptions { ManagedIdentityClientId = managedIdentityClientId });
}
