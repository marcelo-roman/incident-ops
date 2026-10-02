using Azure.Messaging.ServiceBus;
using IncidentOps.Functions.Messaging;

namespace IncidentOps.Functions.Tests.Host;

public sealed class InboundMessagesTests
{
    [Fact]
    public void UsesTheScheduledEnqueueTimeWhenPresent()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "m",
            scheduledEnqueueTime: Sample.Deadline.DueAt,
            enqueuedTime: Sample.Now);

        var inbound = InboundMessages.From(message);

        Assert.Equal("m", inbound.MessageId);
        Assert.Equal(Sample.Deadline.DueAt, inbound.ScheduledFor);
        Assert.Equal("{}"u8.ToArray(), inbound.Body.ToArray());
    }

    [Fact]
    public void FallsBackToTheEnqueuedTime()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("{}"), enqueuedTime: Sample.Now);

        Assert.Equal(Sample.Now, InboundMessages.From(message).ScheduledFor);
    }
}
