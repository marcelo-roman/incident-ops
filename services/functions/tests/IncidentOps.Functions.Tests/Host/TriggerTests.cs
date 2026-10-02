using Azure.Messaging.ServiceBus;
using IncidentOps.Escalation.Application.UseCases;
using IncidentOps.Escalation.Infrastructure.AntiCorruption;
using IncidentOps.Functions.Messaging;
using IncidentOps.Functions.Tests.AntiCorruption;
using IncidentOps.Functions.Tests.Fakes;
using IncidentOps.Functions.Triggers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace IncidentOps.Functions.Tests.Host;

public sealed class TriggerTests
{
    private readonly RecordingMessageActions _actions = new();
    private readonly MessageSettlement _settlement = new(NullLogger<MessageSettlement>.Instance);
    private readonly FakeTimeProvider _time = new(Sample.Now);

    [Fact]
    public async Task ScheduleSlaCheckRunsTheUseCaseAndCompletes()
    {
        var scheduler = new FakeSlaCheckScheduler();
        var useCase = new ScheduleAcknowledgementCheck(new CloudEventIncidentTranslator(), scheduler, _time, NullLogger<ScheduleAcknowledgementCheck>.Instance);

        await new ScheduleSlaCheckTrigger(useCase, _settlement).RunAsync(Message(CloudEvents.Incident()), _actions, CancellationToken.None);

        Assert.Single(scheduler.Scheduled);
        Assert.Equal(["complete"], _actions.Settlements);
    }

    [Fact]
    public async Task NotifyOnCallDeadLettersAnInvalidEnvelope()
    {
        var pager = new FakePager();
        var useCase = new PageOnCall(new CloudEventIncidentTranslator(), new FakeOnCallDirectory(Sample.Rotation()), pager, NullLogger<PageOnCall>.Instance);

        await new NotifyOnCallTrigger(useCase, _settlement).RunAsync(Message("not a cloud event"), _actions, CancellationToken.None);

        Assert.Empty(pager.Pages);
        Assert.Equal(["dead-letter"], _actions.Settlements);
    }

    [Fact]
    public async Task CheckAcknowledgementSlaEscalatesFromTheQueueBody()
    {
        var escalator = new FakeIncidentEscalator();
        var useCase = new CheckAcknowledgementSla(
            new SlaCheckMessageTranslator(),
            new FakeIncidentReader(Sample.State(1)),
            escalator,
            _time,
            NullLogger<CheckAcknowledgementSla>.Instance);

        await new CheckAcknowledgementSlaTrigger(useCase, _settlement)
            .RunAsync(Message($$"""{"incidentId":"{{Sample.IncidentGuid}}","escalationLevel":1}"""), _actions, CancellationToken.None);

        Assert.Single(escalator.Calls);
        Assert.Equal(["complete"], _actions.Settlements);
    }

    private static ServiceBusReceivedMessage Message(string body) => ServiceBusModelFactory.ServiceBusReceivedMessage(
        body: BinaryData.FromString(body),
        messageId: "message-1",
        deliveryCount: 1,
        scheduledEnqueueTime: Sample.Deadline.DueAt);
}
