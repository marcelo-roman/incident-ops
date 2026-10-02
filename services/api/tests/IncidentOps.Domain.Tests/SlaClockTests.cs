using IncidentOps.Domain.Errors;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Domain.Tests;

public class SlaClockTests
{
    private static readonly DateTimeOffset Start = TestData.Now;
    private static readonly SlaTargets Sev1 = SlaPolicy.For(Severity.Sev1);

    [Theory]
    [InlineData(Severity.Sev1, 15, 4 * 60)]
    [InlineData(Severity.Sev2, 30, 8 * 60)]
    [InlineData(Severity.Sev3, 4 * 60, 3 * 24 * 60)]
    [InlineData(Severity.Sev4, 24 * 60, 10 * 24 * 60)]
    public void Policy_targets_follow_the_table(Severity severity, int acknowledgeMinutes, int resolveMinutes)
    {
        var targets = SlaPolicy.For(severity);

        targets.AcknowledgeWithin.Should().Be(TimeSpan.FromMinutes(acknowledgeMinutes));
        targets.ResolveWithin.Should().Be(TimeSpan.FromMinutes(resolveMinutes));
    }

    [Fact]
    public void Time_scale_compresses_both_windows()
    {
        var targets = Sev1.CompressedBy(60);

        targets.AcknowledgeWithin.Should().Be(TimeSpan.FromSeconds(15));
        targets.ResolveWithin.Should().Be(TimeSpan.FromMinutes(4));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void Time_scale_must_be_positive(double timeScale)
    {
        var act = () => Sev1.CompressedBy(timeScale);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Clock_is_immutable_and_restarting_returns_a_new_clock()
    {
        var clock = SlaClock.Start(Sev1, Start);

        var restarted = clock.RestartAcknowledgement(Sev1, Start.AddMinutes(15));

        clock.AcknowledgementBreached.Should().BeFalse();
        clock.AckDueAt.Should().Be(Start.AddMinutes(15));
        restarted.AcknowledgementBreached.Should().BeTrue();
        restarted.AckDueAt.Should().Be(Start.AddMinutes(30));
        restarted.ResolveDueAt.Should().Be(clock.ResolveDueAt);
    }

    [Fact]
    public void Acknowledgement_is_breached_only_after_the_deadline()
    {
        var clock = SlaClock.Start(Sev1, Start);

        clock.RecordAcknowledgement(Start.AddMinutes(15)).AcknowledgementBreached.Should().BeFalse();
        clock.RecordAcknowledgement(Start.AddMinutes(15).AddSeconds(1)).AcknowledgementBreached.Should().BeTrue();
    }

    [Fact]
    public void Open_state_moves_from_on_track_to_at_risk_to_breached()
    {
        var clock = SlaClock.Start(Sev1, Start);

        clock.StateAt(Start.AddMinutes(11), IncidentStatus.Triggered, null, null).Should().Be(SlaState.OnTrack);
        clock.StateAt(Start.AddMinutes(12), IncidentStatus.Triggered, null, null).Should().Be(SlaState.AtRisk);
        clock.StateAt(Start.AddMinutes(16), IncidentStatus.Triggered, null, null).Should().Be(SlaState.Breached);
    }

    [Fact]
    public void Acknowledged_incident_is_measured_against_the_resolve_window()
    {
        var clock = SlaClock.Start(Sev1, Start);
        var acknowledgedAt = Start.AddMinutes(20);

        clock.StateAt(Start.AddMinutes(30), IncidentStatus.Acknowledged, acknowledgedAt, null).Should().Be(SlaState.OnTrack);
        clock.StateAt(Start.AddHours(3).AddMinutes(1), IncidentStatus.Acknowledged, acknowledgedAt, null).Should().Be(SlaState.AtRisk);
        clock.StateAt(Start.AddHours(4).AddMinutes(1), IncidentStatus.Acknowledged, acknowledgedAt, null).Should().Be(SlaState.Breached);
    }

    [Fact]
    public void Compliance_requires_acknowledgement_without_breach_and_resolution_in_time()
    {
        var clock = SlaClock.Start(Sev1, Start);

        clock.Complies(Start.AddMinutes(10), Start.AddHours(3)).Should().BeTrue();
        clock.Complies(null, Start.AddMinutes(5)).Should().BeFalse();
        clock.Complies(Start.AddMinutes(10), null).Should().BeFalse();
        clock.Complies(Start.AddMinutes(10), Start.AddHours(5)).Should().BeFalse();
        clock.RestartAcknowledgement(Sev1, Start.AddMinutes(15)).Complies(Start.AddMinutes(20), Start.AddHours(1)).Should().BeFalse();
    }
}
