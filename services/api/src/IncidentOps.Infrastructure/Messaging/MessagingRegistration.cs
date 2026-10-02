using Azure.Identity;
using Azure.Messaging.ServiceBus;
using IncidentOps.Application.Incidents.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentOps.Infrastructure.Messaging;

internal static class MessagingRegistration
{
    public static IServiceCollection AddEventPublishing(this IServiceCollection services, ServiceBusOptions options)
    {
        var client = CreateClient(options);
        if (client is null)
        {
            return services.AddSingleton<IEventPublisher, LoggingEventPublisher>();
        }

        services.AddSingleton(client);
        services.AddSingleton(provider => provider.GetRequiredService<ServiceBusClient>().CreateSender(options.TopicName));
        return services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();
    }

    private static ServiceBusClient? CreateClient(ServiceBusOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace))
        {
            return new ServiceBusClient(options.FullyQualifiedNamespace, new DefaultAzureCredential());
        }

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new ServiceBusClient(options.ConnectionString);
        }

        return null;
    }
}
