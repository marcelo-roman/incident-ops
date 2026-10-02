using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Application.UseCases;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Functions.Tests.Fakes;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace IncidentOps.Functions.Tests.Application;

public sealed class CheckAcknowledgementSlaTests
{
    private readonly FakeLogger<CheckAcknowledgementSla> _logger = new();

    private CheckAcknowledgementSla UseCase(int watchedLevel, IncidentState? current, FakeIncidentEscalator escalator) => new(
        new FakeAcknowledgementCheckTranslator(Sample.Watch(watchedLevel)),
        new FakeIncidentReader(current),
        escalator,
        new FakeTimeProvider(Sample.Deadline.DueAt.AddSeconds(5)),
        _logger);

    [Fact]
    public async Task EscalatesWhenTheWatchDecidesSo()
    {
        var escalator = new FakeIncidentEscalator();

        var result = await UseCase(1, Sample.State(1), escalator).ExecuteAsync(Messages.Any(), CancellationToken.None);

        Assert.True(result.Acted);
        var call = Assert.Single(escalator.Calls);
        Assert.Equal(Sample.IncidentId, call.IncidentId);
        Assert.Equal("Acknowledgement SLA breached at level 1", call.Reason.Text);
        Assert.Equal(nameof(EscalationAttempt.Escalated), _logger.LatestRecord.GetStructuredStateValue("EscalationAttempt"));
        Assert.Equal("00:00:05", _logger.LatestRecord.GetStructuredStateValue("Overdue"));
    }

    [Theory]
    [InlineData(1, 1, AcknowledgementState.Acknowledged)]
    [InlineData(1, 2, AcknowledgementState.Pending)]
    [InlineData(3, 3, AcknowledgementState.Pending)]
    public async Task DoesNotCallTheApiWhenTheWatchDeclines(int watched, int current, AcknowledgementState acknowledgement)
    {
        var escalator = new FakeIncidentEscalator();

        var result = await UseCase(watched, Sample.State(current, acknowledgement), escalator).ExecuteAsync(Messages.Any(), CancellationToken.None);

        Assert.False(result.Acted);
        Assert.Empty(escalator.Calls);
    }

    [Fact]
    public async Task SkipsUnknownIncidents()
    {
        var escalator = new FakeIncidentEscalator();

        var result = await UseCase(1, null, escalator).ExecuteAsync(Messages.Any(), CancellationToken.None);

        Assert.False(result.Acted);
        Assert.Empty(escalator.Calls);
    }

    [Theory]
    [InlineData(EscalationAttempt.Rejected)]
    [InlineData(EscalationAttempt.NotFound)]
    public async Task TreatsLostRacesAsSkipped(EscalationAttempt attempt)
    {
        var result = await UseCase(1, Sample.State(1), new FakeIncidentEscalator(attempt)).ExecuteAsync(Messages.Any(), CancellationToken.None);

        Assert.False(result.Acted);
    }
}
