using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Domain.Tests;

public class IncidentSlaTests
{
    private static readonly DateTimeOffset Start = TestData.Now;
    private static readonly SlaTargets Sev1 = SlaPolicy.For(Severity.Sev1);
    private static readonly Note Reason = new("No ack", "reason");

    [Fact]
    public void On_time_acknowledgement_and_resolution_is_met()
    {
        var incident = TestData.Incident(Severity.Sev1);
        incident.Acknowledge(TestData.Ava, Start.AddMinutes(10));
        incident.Resolve(TestData.Ava, TestData.RootCause(), Start.AddHours(3));

        incident.CompliesWithSla().Should().BeTrue();
        incident.SlaStateAt(Start.AddDays(30)).Should().Be(SlaState.Met);
    }

    [Fact]
    public void Late_acknowledgement_breaches_even_when_resolved_in_time()
    {
        var incident = TestData.Incident(Severity.Sev1);
        incident.Acknowledge(TestData.Ava, Start.AddMinutes(16));
        incident.Resolve(TestData.Ava, TestData.RootCause(), Start.AddHours(1));

        incident.Sla.AcknowledgementBreached.Should().BeTrue();
        incident.CompliesWithSla().Should().BeFalse();
        incident.SlaStateAt(Start.AddDays(1)).Should().Be(SlaState.Breached);
    }

    [Fact]
    public void Late_resolution_breaches()
    {
        var incident = TestData.Incident(Severity.Sev1);
        incident.Acknowledge(TestData.Ava, Start.AddMinutes(5));
        incident.Resolve(TestData.Ava, TestData.RootCause(), Start.AddHours(5));

        incident.SlaStateAt(Start.AddDays(30)).Should().Be(SlaState.Breached);
    }

    [Fact]
    public void Resolution_without_acknowledgement_is_breached()
    {
        var incident = TestData.Incident(Severity.Sev1);
        incident.Resolve(TestData.Ava, TestData.RootCause("False positive"), Start.AddMinutes(5));

        incident.Sla.AcknowledgementBreached.Should().BeFalse();
        incident.CompliesWithSla().Should().BeFalse();
        incident.SlaStateAt(Start.AddHours(1)).Should().Be(SlaState.Breached);
    }

    [Fact]
    public void Escalated_twice_then_acknowledged_in_the_last_window_stays_breached()
    {
        var incident = TestData.Incident(Severity.Sev1);
        incident.Escalate(Reason, TestData.Shift, Sev1, Start.AddMinutes(15));
        incident.Escalate(Reason, TestData.Shift, Sev1, Start.AddMinutes(30));
        incident.Acknowledge(new Actor("Lead"), Start.AddMinutes(35));
        incident.Mitigate(new Actor("Lead"), TestData.Note("Rolled back"), Start.AddMinutes(50));
        incident.Resolve(new Actor("Lead"), TestData.RootCause(), Start.AddHours(1));

        incident.AcknowledgedAt.Should().BeBefore(incident.Sla.AckDueAt);
        incident.Sla.AcknowledgementBreached.Should().BeTrue();
        incident.SlaStateAt(Start.AddHours(2)).Should().Be(SlaState.Breached);
    }

    [Fact]
    public void Escalation_opens_a_new_live_window()
    {
        var incident = TestData.Incident(Severity.Sev1);
        var escalatedAt = Start.AddMinutes(15);
        incident.Escalate(Reason, TestData.Shift, Sev1, escalatedAt);

        incident.SlaStateAt(escalatedAt.AddMinutes(1)).Should().Be(SlaState.OnTrack);
        incident.SlaStateAt(escalatedAt.AddMinutes(12)).Should().Be(SlaState.AtRisk);
        incident.SlaStateAt(escalatedAt.AddMinutes(16)).Should().Be(SlaState.Breached);
    }
}
