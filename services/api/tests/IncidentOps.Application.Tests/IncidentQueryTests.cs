using IncidentOps.Application.Catalog;
using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents;
using IncidentOps.Application.Incidents.Details;
using IncidentOps.Application.Incidents.Export;
using IncidentOps.Application.Incidents.Listing;
using IncidentOps.Application.Incidents.Notes;
using IncidentOps.Application.Incidents.Trigger;
using IncidentOps.Application.Metrics;
using IncidentOps.Application.OnCall;
using IncidentOps.Application.Tests.Fakes;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Tests;

public sealed class IncidentQueryTests : IDisposable
{
    private readonly ApplicationHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task Details_read_the_projection_with_the_timeline_in_order()
    {
        var incident = await TriggerAsync(Severity.Sev2, "checkout");
        await _harness.Get<AddIncidentNoteHandler>().HandleAsync(incident.Id, new AddIncidentNote("Ava", "First"), CancellationToken.None);

        var details = await _harness.Get<GetIncidentHandler>().HandleAsync(incident.Id, CancellationToken.None);

        details.Id.Should().Be(incident.Id);
        details.Timeline.Select(entry => entry.Kind).Should().Equal(TimelineKind.Triggered, TimelineKind.Note);
    }

    [Fact]
    public async Task Details_of_unknown_incident_fail_with_not_found()
    {
        var act = () => _harness.Get<GetIncidentHandler>().HandleAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<IncidentNotFoundException>();
    }

    [Fact]
    public async Task List_applies_filters_newest_first()
    {
        await TriggerAsync(Severity.Sev2, "checkout");
        _harness.Clock.Advance(TimeSpan.FromMinutes(1));
        var newest = await TriggerAsync(Severity.Sev2, "identity");
        _harness.Clock.Advance(TimeSpan.FromMinutes(1));
        await TriggerAsync(Severity.Sev4, "identity");

        var matches = await _harness.Get<ListIncidentsHandler>().HandleAsync(
            new ListIncidents(null, Severity.Sev2, null, true, null),
            CancellationToken.None);

        matches.Should().HaveCount(2);
        matches[0].Id.Should().Be(newest.Id);
    }

    [Theory]
    [InlineData(null, ListIncidents.DefaultLimit)]
    [InlineData(0, 1)]
    [InlineData(10_000, ListIncidents.MaxLimit)]
    public void List_limit_is_clamped(int? requested, int expected)
    {
        new ListIncidents(null, null, null, null, requested).ToFilter().Limit.Should().Be(expected);
    }

    [Fact]
    public void Export_defaults_to_the_last_180_days_and_rejects_long_ranges()
    {
        var now = _harness.Clock.UtcNow;

        var (from, to) = new ExportIncidents(null, null).RangeEndingAt(now);
        var tooLong = () => new ExportIncidents(now.AddDays(-500), now).RangeEndingAt(now);

        to.Should().Be(now);
        from.Should().Be(now.AddDays(-180));
        tooLong.Should().Throw<RequestValidationException>();
    }

    [Fact]
    public async Task Export_returns_incidents_in_the_range()
    {
        var inside = await TriggerAsync(Severity.Sev3, "checkout");

        var exported = await _harness.Get<ExportIncidentsHandler>().HandleAsync(
            new ExportIncidents(_harness.Clock.UtcNow.AddHours(-1), null),
            CancellationToken.None);

        exported.Should().ContainSingle().Which.Id.Should().Be(inside.Id);
    }

    [Fact]
    public async Task Metrics_summary_counts_open_incidents()
    {
        await TriggerAsync(Severity.Sev1, "checkout");
        await TriggerAsync(Severity.Sev3, "identity");

        var summary = await _harness.Get<GetMetricsSummaryHandler>().HandleAsync(CancellationToken.None);

        summary.OpenBySeverity[Severity.Sev1].Should().Be(1);
        summary.OpenBySeverity[Severity.Sev3].Should().Be(1);
        summary.BreachedOpen.Should().Be(0);
    }

    [Fact]
    public async Task Current_on_call_comes_from_the_rotation()
    {
        var onCall = await _harness.Get<GetCurrentOnCallHandler>().HandleAsync(CancellationToken.None);

        onCall.Primary.Should().BeOneOf(FakeOnCallRotationRepository.Engineers);
        onCall.Lead.Should().Be("Lead");
        onCall.WeekStart.Should().BeBefore(_harness.Clock.UtcNow);
    }

    [Fact]
    public async Task Services_are_listed_from_the_catalog_projection()
    {
        var services = await _harness.Get<ListServicesHandler>().HandleAsync(CancellationToken.None);

        services.Select(service => service.Id).Should().Contain("platform");
    }

    private Task<IncidentView> TriggerAsync(Severity severity, string serviceId) =>
        _harness.Get<TriggerIncidentHandler>().HandleAsync(
            new TriggerIncident("Incident", "Description", serviceId, severity),
            CancellationToken.None);
}
