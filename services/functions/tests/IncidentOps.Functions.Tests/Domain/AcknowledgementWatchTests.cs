using IncidentOps.Escalation.Domain;
using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Functions.Tests.Domain;

public sealed class AcknowledgementWatchTests
{
    public static TheoryData<int, AcknowledgementState, Type> Openings => new()
    {
        { 1, AcknowledgementState.Pending, typeof(WatchOpening.Opened) },
        { 2, AcknowledgementState.Pending, typeof(WatchOpening.Opened) },
        { 3, AcknowledgementState.Pending, typeof(WatchOpening.NotRequired) },
        { 1, AcknowledgementState.Acknowledged, typeof(WatchOpening.NotRequired) },
        { 2, AcknowledgementState.Acknowledged, typeof(WatchOpening.NotRequired) },
    };

    public static TheoryData<int, int, AcknowledgementState, Type> Evaluations => new()
    {
        { 1, 1, AcknowledgementState.Pending, typeof(EscalationDecision.Escalate) },
        { 2, 2, AcknowledgementState.Pending, typeof(EscalationDecision.Escalate) },
        { 1, 1, AcknowledgementState.Acknowledged, typeof(EscalationDecision.AlreadyAcknowledged) },
        { 2, 3, AcknowledgementState.Acknowledged, typeof(EscalationDecision.AlreadyAcknowledged) },
        { 1, 2, AcknowledgementState.Pending, typeof(EscalationDecision.Superseded) },
        { 2, 3, AcknowledgementState.Pending, typeof(EscalationDecision.Superseded) },
        { 3, 3, AcknowledgementState.Pending, typeof(EscalationDecision.FinalLevelReached) },
    };

    [Theory]
    [MemberData(nameof(Openings))]
    public void OpensOnlyWhileAcknowledgementIsPendingBelowTheFinalLevel(int level, AcknowledgementState acknowledgement, Type expected)
    {
        Assert.IsType(expected, AcknowledgementWatch.Open(Sample.Window(level, acknowledgement)));
    }

    [Fact]
    public void OpenedWatchCarriesTheWindow()
    {
        var opening = Assert.IsType<WatchOpening.Opened>(AcknowledgementWatch.Open(Sample.Window(2)));

        Assert.Equal(Sample.IncidentId, opening.Watch.IncidentId);
        Assert.Equal(EscalationLevel.Secondary, opening.Watch.Level);
        Assert.Equal(Sample.Deadline, opening.Watch.Deadline);
    }

    [Fact]
    public void SchedulesTheCheckAtTheDeadline()
    {
        var check = Sample.Watch(2).Schedule(Sample.Now);

        Assert.Equal(Sample.Deadline.DueAt, check.CheckAt);
        Assert.Equal($"{Sample.IncidentGuid}-2", check.Key.ToString());
    }

    [Fact]
    public void SchedulesImmediatelyOnceTheDeadlinePassed()
    {
        var later = Sample.Deadline.DueAt.AddMinutes(10);

        Assert.Equal(later, Sample.Watch().Schedule(later).CheckAt);
    }

    [Theory]
    [MemberData(nameof(Evaluations))]
    public void EvaluatesTheCurrentIncidentState(int watched, int current, AcknowledgementState acknowledgement, Type expected)
    {
        var decision = Sample.Watch(watched).Evaluate(Sample.State(current, acknowledgement), Sample.Deadline.DueAt);

        Assert.IsType(expected, decision);
    }

    [Fact]
    public void EscalationCarriesReasonAndLateness()
    {
        var decision = Sample.Watch(1).Evaluate(Sample.State(1), Sample.Deadline.DueAt.AddSeconds(30));

        var escalate = Assert.IsType<EscalationDecision.Escalate>(decision);
        Assert.Equal("Acknowledgement SLA breached at level 1", escalate.Reason.Text);
        Assert.Equal(TimeSpan.FromSeconds(30), escalate.Overdue);
    }

    [Fact]
    public void RefusesToEvaluateAnotherIncident()
    {
        var other = new IncidentState(IncidentId.From(Guid.NewGuid()), EscalationLevel.Primary, AcknowledgementState.Pending);

        Assert.Throws<DomainException>(() => Sample.Watch().Evaluate(other, Sample.Now));
    }
}
