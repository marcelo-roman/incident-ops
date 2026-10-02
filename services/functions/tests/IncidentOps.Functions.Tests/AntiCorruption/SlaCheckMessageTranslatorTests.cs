using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Infrastructure.AntiCorruption;

namespace IncidentOps.Functions.Tests.AntiCorruption;

public sealed class SlaCheckMessageTranslatorTests
{
    private readonly SlaCheckMessageTranslator _translator = new();

    [Fact]
    public void ResumesTheWatchFromTheContractBody()
    {
        var body = $$"""{"incidentId":"{{Sample.IncidentGuid}}","escalationLevel":2}""";

        var watch = _translator.Translate(CloudEvents.Message(body, Sample.Deadline.DueAt));

        Assert.Equal(Sample.IncidentId, watch.IncidentId);
        Assert.Equal(EscalationLevel.Secondary, watch.Level);
        Assert.Equal(Sample.Deadline, watch.Deadline);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("""{"escalationLevel":1}""")]
    [InlineData("""{"incidentId":"6f1c2a52-8f0e-4bfa-9d3c-3f7b3f3a1b11","escalationLevel":0}""")]
    [InlineData("""{"incidentId":"6f1c2a52-8f0e-4bfa-9d3c-3f7b3f3a1b11","escalationLevel":4}""")]
    [InlineData("""{"incidentId":"not-a-guid","escalationLevel":1}""")]
    public void RejectsInvalidBodies(string body)
    {
        Assert.Throws<MalformedMessageException>(() => _translator.Translate(CloudEvents.Message(body)));
    }

    [Fact]
    public void WritesTheContractBody()
    {
        var message = SlaCheckMessageTranslator.ToMessage(Sample.Watch(2).Schedule(Sample.Now));

        Assert.Equal(new SlaCheckMessage(Sample.IncidentGuid, 2), message);
    }
}
