using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Escalation.Domain.Watches;
using IncidentOps.Escalation.Infrastructure.AntiCorruption;

namespace IncidentOps.Functions.Tests.AntiCorruption;

public sealed class CloudEventIncidentTranslatorTests
{
    private readonly CloudEventIncidentTranslator _translator = new();

    [Fact]
    public void TranslatesTheIncidentIntoTheEscalationModel()
    {
        var translation = _translator.Translate(CloudEvents.Message(CloudEvents.Incident("incident.escalated", level: 2, severity: "Sev2")));

        var window = Assert.IsType<Translation<AcknowledgementWindowOpened>.Accepted>(translation).Value;
        Assert.Equal(Sample.IncidentId, window.Incident.Id);
        Assert.Equal(IncidentNumber.From(1042), window.Incident.Number);
        Assert.Equal("Checkout returns 502", window.Incident.Title);
        Assert.Equal(Severity.Sev2, window.Incident.Severity);
        Assert.Equal(ServiceId.From("checkout"), window.Incident.ServiceId);
        Assert.Equal(EscalationLevel.Secondary, window.Level);
        Assert.Equal(new DateTimeOffset(2026, 10, 2, 14, 15, 0, TimeSpan.Zero), window.Deadline.DueAt);
        Assert.Equal(AcknowledgementState.Pending, window.Acknowledgement);
    }

    [Theory]
    [InlineData("Triggered", AcknowledgementState.Pending)]
    [InlineData("Acknowledged", AcknowledgementState.Acknowledged)]
    [InlineData("Mitigated", AcknowledgementState.Acknowledged)]
    [InlineData("Resolved", AcknowledgementState.Acknowledged)]
    public void MapsUpstreamStatusToAcknowledgement(string status, AcknowledgementState expected)
    {
        var translation = _translator.Translate(CloudEvents.Message(CloudEvents.Incident(status: status)));

        Assert.Equal(expected, Assert.IsType<Translation<AcknowledgementWindowOpened>.Accepted>(translation).Value.Acknowledgement);
    }

    [Theory]
    [InlineData("incident.acknowledged")]
    [InlineData("incident.mitigated")]
    [InlineData("incident.resolved")]
    public void IgnoresEventsThatDoNotOpenAWindow(string type)
    {
        var translation = _translator.Translate(CloudEvents.Message(CloudEvents.Incident(type, "Resolved")));

        Assert.IsType<Translation<AcknowledgementWindowOpened>.Ignored>(translation);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"specversion":"0.3","id":"1","type":"incident.triggered","source":"incident-ops-api","data":{}}""")]
    [InlineData("""{"specversion":"1.0","id":"1","type":"incident.triggered","source":"incident-ops-api"}""")]
    public void RejectsInvalidEnvelopes(string body)
    {
        Assert.Throws<MalformedMessageException>(() => _translator.Translate(CloudEvents.Message(body)));
    }

    [Theory]
    [InlineData("Triggered", 0, "Sev1")]
    [InlineData("Triggered", 4, "Sev1")]
    [InlineData("Paused", 1, "Sev1")]
    [InlineData("Triggered", 1, "Sev9")]
    public void RejectsIncidentsThatBreakTheModel(string status, int level, string severity)
    {
        var body = CloudEvents.Incident(status: status, level: level, severity: severity);

        Assert.Throws<MalformedMessageException>(() => _translator.Translate(CloudEvents.Message(body)));
    }
}
