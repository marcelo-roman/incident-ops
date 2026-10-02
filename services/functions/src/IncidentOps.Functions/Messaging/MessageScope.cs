using Azure.Messaging.ServiceBus;

namespace IncidentOps.Functions.Messaging;

public static class MessageScope
{
    public static IReadOnlyDictionary<string, object> For(ServiceBusReceivedMessage message) => new Dictionary<string, object>
    {
        ["MessageId"] = message.MessageId,
        ["CorrelationId"] = message.CorrelationId ?? message.MessageId,
        ["DeliveryCount"] = message.DeliveryCount,
        ["EventType"] = EventType(message),
    };

    private static object EventType(ServiceBusReceivedMessage message)
    {
        if (message.ApplicationProperties.TryGetValue("eventType", out var eventType) && eventType is not null)
        {
            return eventType;
        }

        return message.Subject ?? string.Empty;
    }
}
