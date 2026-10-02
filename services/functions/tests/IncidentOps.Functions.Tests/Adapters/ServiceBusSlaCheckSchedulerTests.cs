using IncidentOps.Escalation.Infrastructure.ServiceBus;
using IncidentOps.Functions.Tests.Fakes;

namespace IncidentOps.Functions.Tests.Adapters;

public sealed class ServiceBusSlaCheckSchedulerTests
{
    [Fact]
    public async Task SchedulesTheContractBodyWithTheWatchKeyAsMessageId()
    {
        var sender = new RecordingServiceBusSender();
        var check = Sample.Watch(2).Schedule(Sample.Now);

        await new ServiceBusSlaCheckScheduler(sender).ScheduleAsync(check, CancellationToken.None);

        var (message, enqueueAt) = Assert.Single(sender.Scheduled);
        Assert.Equal(Sample.Deadline.DueAt, enqueueAt);
        Assert.Equal($"{Sample.IncidentGuid}-2", message.MessageId);
        Assert.Equal("application/json", message.ContentType);
        Assert.Equal($$"""{"incidentId":"{{Sample.IncidentGuid}}","escalationLevel":2}""", message.Body.ToString());
    }
}
