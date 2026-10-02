using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Escalation.Domain.Paging;

namespace IncidentOps.Functions.Tests.Domain;

public sealed class PagingDecisionTests
{
    public static TheoryData<string, AcknowledgementState, Type> Decisions => new()
    {
        { "Sev1", AcknowledgementState.Pending, typeof(PagingDecision.Page) },
        { "Sev2", AcknowledgementState.Pending, typeof(PagingDecision.Page) },
        { "Sev3", AcknowledgementState.Pending, typeof(PagingDecision.DoNotPage) },
        { "Sev4", AcknowledgementState.Pending, typeof(PagingDecision.DoNotPage) },
        { "Sev1", AcknowledgementState.Acknowledged, typeof(PagingDecision.DoNotPage) },
    };

    [Theory]
    [MemberData(nameof(Decisions))]
    public void PagesSev1AndSev2WhileAwaitingAcknowledgement(string severity, AcknowledgementState acknowledgement, Type expected)
    {
        var decision = PagingDecision.Decide(Sample.Window(1, acknowledgement, Severity.Parse(severity)), Sample.Rotation());

        Assert.IsType(expected, decision);
    }

    [Theory]
    [InlineData(1, "ana.silva")]
    [InlineData(2, "bruno.costa")]
    [InlineData(3, "carla.mendes")]
    public void PagesWhoeverIsOnCallForTheLevel(int level, string target)
    {
        var page = Assert.IsType<PagingDecision.Page>(PagingDecision.Decide(Sample.Window(level), Sample.Rotation()));

        Assert.Equal(OnCallTarget.Named(target), page.Request.Target);
        Assert.Equal(EscalationLevel.From(level), page.Request.Level);
        Assert.Equal(Sample.Profile(), page.Request.Incident);
    }
}
