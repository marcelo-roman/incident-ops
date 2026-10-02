using IncidentOps.Escalation.Application.UseCases;
using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Functions.Tests.Fakes;
using Microsoft.Extensions.Logging.Testing;

namespace IncidentOps.Functions.Tests.Application;

public sealed class PageOnCallTests
{
    private readonly FakePager _pager = new();
    private readonly FakeOnCallDirectory _directory = new(Sample.Rotation());
    private readonly FakeLogger<PageOnCall> _logger = new();

    private PageOnCall UseCase(FakeIncidentEventTranslator translator) => new(translator, _directory, _pager, _logger);

    [Fact]
    public async Task PagesTheTargetChosenByTheDomain()
    {
        var result = await UseCase(FakeIncidentEventTranslator.Accepting(Sample.Window(2))).ExecuteAsync(Messages.Any(), CancellationToken.None);

        Assert.True(result.Acted);
        var page = Assert.Single(_pager.Pages);
        Assert.Equal(OnCallTarget.Named("bruno.costa"), page.Target);
        Assert.Equal("bruno.costa", _logger.LatestRecord.GetStructuredStateValue("Target"));
    }

    [Fact]
    public async Task DoesNotPageWhenTheDomainDeclines()
    {
        var translator = FakeIncidentEventTranslator.Accepting(Sample.Window(1, severity: Severity.Sev3));

        var result = await UseCase(translator).ExecuteAsync(Messages.Any(), CancellationToken.None);

        Assert.False(result.Acted);
        Assert.Empty(_pager.Pages);
    }

    [Fact]
    public async Task IgnoresEventsWithoutReadingTheRotation()
    {
        var result = await UseCase(FakeIncidentEventTranslator.Ignoring("incident.mitigated does not open an acknowledgement window"))
            .ExecuteAsync(Messages.Any(), CancellationToken.None);

        Assert.False(result.Acted);
        Assert.Equal(0, _directory.Calls);
        Assert.Empty(_pager.Pages);
    }
}
