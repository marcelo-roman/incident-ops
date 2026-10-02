using Azure.Messaging.ServiceBus;
using IncidentOps.Escalation.Application.Messaging;

namespace IncidentOps.Functions.Messaging;

public static class InboundMessages
{
    public static InboundMessage From(ServiceBusReceivedMessage message) =>
        new(message.MessageId, message.Body.ToMemory(), ScheduledFor(message));

    private static DateTimeOffset ScheduledFor(ServiceBusReceivedMessage message)
    {
        if (message.ScheduledEnqueueTime != default)
        {
            return message.ScheduledEnqueueTime;
        }

        return message.EnqueuedTime;
    }
}
