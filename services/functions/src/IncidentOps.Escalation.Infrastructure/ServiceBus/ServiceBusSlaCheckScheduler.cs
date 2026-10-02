using Azure.Messaging.ServiceBus;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Watches;
using IncidentOps.Escalation.Infrastructure.AntiCorruption;

namespace IncidentOps.Escalation.Infrastructure.ServiceBus;

public sealed class ServiceBusSlaCheckScheduler(ServiceBusSender sender) : ISlaCheckScheduler
{
    public Task ScheduleAsync(ScheduledCheck check, CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(SlaCheckMessageTranslator.ToMessage(check), WireJson.Options))
        {
            MessageId = check.Key.ToString(),
            Subject = "sla-check",
            ContentType = "application/json",
        };
        return sender.ScheduleMessageAsync(message, check.CheckAt, cancellationToken);
    }
}
