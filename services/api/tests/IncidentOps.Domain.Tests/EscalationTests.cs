using IncidentOps.Domain.Errors;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Incidents.Events;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Domain.Tests;

public class EscalationTests
{
    private static readonly SlaTargets Sev1 = SlaPolicy.For(Severity.Sev1);
    private static readonly Note Reason = new("Not acknowledged", "reason");

    [Fact]
    public void Escalation_moves_to_the_next_target_restarts_the_ack_window_and_raises_escalated()
    {
        var incident = TestData.Incident(Severity.Sev1);
        var at = TestData.Now.AddMinutes(15);

        incident.Escalate(Reason, TestData.Shift, Sev1, at);

        incident.EscalationLevel.Value.Should().Be(2);
        incident.Assignee.Should().Be(new Actor("Secondary"));
        incident.Sla.AckWindowStartsAt.Should().Be(at);
        incident.Sla.AckDueAt.Should().Be(at.AddMinutes(15));
        incident.Sla.ResolveDueAt.Should().Be(TestData.Now.AddHours(4));
        incident.Timeline[^1].Message.Should().Contain("level 2").And.Contain("Secondary");
        incident.DomainEvents[^1].Should().BeOfType<IncidentEscalated>().Which.Level.Value.Should().Be(2);
    }

    [Fact]
    public void Third_level_targets_the_engineering_lead_and_stops()
    {
        var incident = TestData.Incident(Severity.Sev1);
        incident.Escalate(Reason, TestData.Shift, Sev1, TestData.Now.AddMinutes(15));
        incident.Escalate(Reason, TestData.Shift, Sev1, TestData.Now.AddMinutes(30));

        var act = () => incident.Escalate(Reason, TestData.Shift, Sev1, TestData.Now.AddMinutes(45));

        incident.Assignee.Should().Be(new Actor("Lead"));
        act.Should().Throw<EscalationNotAllowedException>();
        incident.EscalationLevel.IsLast.Should().BeTrue();
    }

    [Fact]
    public void Acknowledged_incident_does_not_escalate()
    {
        var incident = TestData.Incident(Severity.Sev1);
        incident.Acknowledge(TestData.Ava, TestData.Now.AddMinutes(1));

        var act = () => incident.Escalate(Reason, TestData.Shift, Sev1, TestData.Now.AddMinutes(15));

        act.Should().Throw<EscalationNotAllowedException>();
    }

    [Theory]
    [InlineData(1, "Primary")]
    [InlineData(2, "Secondary")]
    [InlineData(3, "Lead")]
    public void Each_level_has_a_target(int level, string target)
    {
        TestData.Shift.TargetFor(new EscalationLevel(level)).Should().Be(new Actor(target));
    }
}
