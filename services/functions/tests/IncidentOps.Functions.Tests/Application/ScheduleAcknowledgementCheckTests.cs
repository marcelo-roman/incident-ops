using IncidentOps.Escalation.Application.UseCases;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Functions.Tests.Fakes;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace IncidentOps.Functions.Tests.Application;

public sealed class ScheduleAcknowledgementCheckTests
{
    private readonly FakeSlaCheckScheduler _scheduler = new();
    private readonly FakeLogger<ScheduleAcknowledgementCheck> _logger = new();

    private ScheduleAcknowledgementCheck UseCase(FakeIncidentEventTranslator translator) =>
        new(translator, _scheduler, new FakeTimeProvider(Sample.Now), _logger);

    [Fact]
    public async Task SchedulesTheWatchOpenedByTheEvent()
    {
        var result = await UseCase(FakeIncidentEventTranslator.Accepting(Sample.Window(2))).ExecuteAsync(Messages.Any(), CancellationToken.None);

        Assert.True(result.Acted);
        var check = Assert.Single(_scheduler.Scheduled);
        Assert.Equal($"{Sample.IncidentGuid}-2", check.Key.ToString());
        Assert.Equal(Sample.Deadline.DueAt, check.CheckAt);
        Assert.Equal(Sample.IncidentGuid.ToString(), _logger.LatestRecord.GetStructuredStateValue("IncidentId"));
        Assert.Equal("2", _logger.LatestRecord.GetStructuredStateValue("EscalationLevel"));
    }

    [Fact]
    public async Task SkipsWhenTheDomainDoesNotOpenAWatch()
    {
        var translator = FakeIncidentEventTranslator.Accepting(Sample.Window(1, AcknowledgementState.Acknowledged));

        var result = await UseCase(translator).ExecuteAsync(Messages.Any(), CancellationToken.None);

        Assert.False(result.Acted);
        Assert.Empty(_scheduler.Scheduled);
    }

    [Fact]
    public async Task SkipsEventsTheTranslatorIgnores()
    {
        var result = await UseCase(FakeIncidentEventTranslator.Ignoring("incident.resolved does not open an acknowledgement window"))
            .ExecuteAsync(Messages.Any(), CancellationToken.None);

        Assert.False(result.Acted);
        Assert.Equal("incident.resolved does not open an acknowledgement window", result.Description);
        Assert.Empty(_scheduler.Scheduled);
    }
}
