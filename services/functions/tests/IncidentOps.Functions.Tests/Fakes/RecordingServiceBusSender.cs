using Azure.Messaging.ServiceBus;

namespace IncidentOps.Functions.Tests.Fakes;

internal sealed class RecordingServiceBusSender : ServiceBusSender
{
    public List<(ServiceBusMessage Message, DateTimeOffset EnqueueAt)> Scheduled { get; } = [];

    public override Task<long> ScheduleMessageAsync(
        ServiceBusMessage message,
        DateTimeOffset scheduledEnqueueTime,
        CancellationToken cancellationToken = default)
    {
        Scheduled.Add((message, scheduledEnqueueTime));
        return Task.FromResult((long)Scheduled.Count);
    }
}
