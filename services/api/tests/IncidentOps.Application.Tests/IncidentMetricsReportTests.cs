using IncidentOps.Application.Metrics;
using IncidentOps.Application.Tests.Fakes;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.OnCall;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Application.Tests;

public class IncidentMetricsReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 14, 0, 0, TimeSpan.Zero);
    private static readonly Actor Ava = new("Ava");
    private static readonly OnCallShift Shift = new(Now.AddDays(-2), new Actor("Primary"), new Actor("Secondary"), new Actor("Lead"));

    private static Incident Incident(Severity severity, DateTimeOffset at) =>
        Domain.Incidents.Incident.Trigger(
            new IncidentDraft(new IncidentTitle("Title"), new Description(null), new ServiceId("checkout"), severity),
            new IncidentOpening(new IncidentNumber(1001), Shift, SlaPolicy.For(severity), at));

    private static Incident Compliant(DateTimeOffset at)
    {
        var incident = Incident(Severity.Sev1, at);
        incident.Acknowledge(Ava, at.AddMinutes(10));
        incident.Resolve(Ava, new RootCause("Fixed"), at.AddHours(2));
        return incident;
    }

    private static MetricsSummaryView Summarize(params Incident[] incidents) =>
        IncidentMetricsReport.Summarize(incidents.Select(InMemoryIncidentQueries.Project).ToList(), Now);

    [Fact]
    public void Summarizes_open_incidents_compliance_and_response_times()
    {
        var late = Incident(Severity.Sev1, Now.AddDays(-2));
        late.Acknowledge(Ava, Now.AddDays(-2).AddMinutes(20));
        late.Resolve(Ava, new RootCause("Fixed"), Now.AddDays(-2).AddHours(6));
        var breachedOpen = Incident(Severity.Sev2, Now.AddDays(-1));
        var onTrack = Incident(Severity.Sev3, Now.AddMinutes(-5));
        var old = Incident(Severity.Sev1, Now.AddDays(-60));
        old.Resolve(Ava, new RootCause("Fixed"), Now.AddDays(-60).AddHours(1));

        var summary = Summarize(Compliant(Now.AddDays(-3)), late, breachedOpen, onTrack, old);

        summary.OpenBySeverity.Should().BeEquivalentTo(new Dictionary<Severity, int>
        {
            [Severity.Sev1] = 0,
            [Severity.Sev2] = 1,
            [Severity.Sev3] = 1,
            [Severity.Sev4] = 0,
        });
        summary.BreachedOpen.Should().Be(1);
        summary.SlaCompliance30d.Should().Be(33.3);
        summary.Mtta30dMinutes.Should().Be(15);
        summary.Mttr30dMinutes.Should().Be(240);
    }

    [Fact]
    public void Open_incident_with_a_breached_deadline_counts_as_non_compliant()
    {
        Summarize(Compliant(Now.AddDays(-2)), Incident(Severity.Sev1, Now.AddHours(-1))).SlaCompliance30d.Should().Be(50);
    }

    [Fact]
    public void Open_incidents_inside_their_deadlines_are_not_counted_yet()
    {
        var acknowledged = Incident(Severity.Sev3, Now.AddHours(-2));
        acknowledged.Acknowledge(Ava, Now.AddHours(-1));

        Summarize(Compliant(Now.AddDays(-2)), Incident(Severity.Sev1, Now.AddMinutes(-1)), acknowledged).SlaCompliance30d.Should().Be(100);
    }

    [Fact]
    public void Escalated_incident_never_counts_as_compliant()
    {
        var escalated = Incident(Severity.Sev1, Now.AddDays(-1));
        escalated.Escalate(new Note("No ack"), Shift, SlaPolicy.For(Severity.Sev1), Now.AddDays(-1).AddMinutes(15));
        escalated.Acknowledge(Ava, Now.AddDays(-1).AddMinutes(20));
        escalated.Resolve(Ava, new RootCause("Fixed"), Now.AddDays(-1).AddHours(1));

        Summarize(escalated).SlaCompliance30d.Should().Be(0);
    }

    [Fact]
    public void Empty_window_reports_full_compliance()
    {
        var summary = Summarize();

        summary.SlaCompliance30d.Should().Be(100);
        summary.Mtta30dMinutes.Should().Be(0);
        summary.OpenBySeverity.Values.Should().AllSatisfy(count => count.Should().Be(0));
    }
}
