using System.Text.Json;
using IncidentOps.Application.Common;
using IncidentOps.Application.Common.Outbox;

namespace IncidentOps.Application.Incidents.Messaging;

public sealed class IncidentChangePublisher(IEventPublisher publisher, IEnumerable<IIncidentNotifier> notifiers) : IOutboxMessageHandler
{
    public async Task HandleAsync(string type, string payload, CancellationToken cancellationToken)
    {
        if (type != IncidentChangeMessage.MessageType)
        {
            throw new InvalidOperationException($"Outbox message type '{type}' has no handler.");
        }

        var message = JsonSerializer.Deserialize<IncidentChangeMessage>(payload, ContractJson.Options)
            ?? throw new InvalidOperationException("Outbox payload is empty.");

        foreach (var notifier in notifiers)
        {
            await notifier.NotifyAsync(message, cancellationToken);
        }

        await PublishAsync(message, cancellationToken);
    }

    private Task PublishAsync(IncidentChangeMessage message, CancellationToken cancellationToken)
    {
        if (message.EventType is null)
        {
            return Task.CompletedTask;
        }

        var integrationEvent = new IncidentIntegrationEvent(message.Id, message.EventType, message.OccurredAt, message.Incident);
        return publisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
