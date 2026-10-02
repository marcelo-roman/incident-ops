using Azure.Messaging.ServiceBus;
using IncidentOps.Application.Incidents.Messaging;
using Microsoft.Extensions.Logging;

namespace IncidentOps.Infrastructure.Messaging;

internal sealed partial class ServiceBusEventPublisher(
    ServiceBusSender sender,
    ILogger<ServiceBusEventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync(IncidentIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await sender.SendMessageAsync(CloudEventMessageFactory.Create(integrationEvent), cancellationToken);
        LogPublished(integrationEvent.Type, integrationEvent.Incident.Number);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {EventType} for INC-{IncidentNumber}")]
    private partial void LogPublished(string eventType, int incidentNumber);
}
