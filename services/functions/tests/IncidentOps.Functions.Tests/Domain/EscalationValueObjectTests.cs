using IncidentOps.Escalation.Domain;
using IncidentOps.Escalation.Domain.Escalation;

namespace IncidentOps.Functions.Tests.Domain;

public sealed class EscalationValueObjectTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void EscalationLevelStaysWithinOneToThree(int value)
    {
        Assert.Throws<DomainException>(() => EscalationLevel.From(value));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public void EscalationLevelAdvancesOneStep(int from, int to)
    {
        Assert.Equal(EscalationLevel.From(to), EscalationLevel.From(from).Next());
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void OnlyLevelThreeIsFinal(int value, bool isFinal)
    {
        Assert.Equal(isFinal, EscalationLevel.From(value).IsFinal);
    }

    [Fact]
    public void FinalLevelHasNoNext()
    {
        Assert.Throws<DomainException>(() => EscalationLevel.Lead.Next());
    }

    [Fact]
    public void DeadlineKeepsAFutureDueTime()
    {
        var deadline = AcknowledgementDeadline.At(Sample.Now.AddMinutes(5));

        Assert.False(deadline.HasPassedAt(Sample.Now));
        Assert.Equal(Sample.Now.AddMinutes(5), deadline.CheckTimeAt(Sample.Now));
        Assert.Equal(TimeSpan.Zero, deadline.OverdueAt(Sample.Now));
    }

    [Fact]
    public void DeadlineInThePastIsCheckedNow()
    {
        var deadline = AcknowledgementDeadline.At(Sample.Now.AddMinutes(-5));

        Assert.True(deadline.HasPassedAt(Sample.Now));
        Assert.Equal(Sample.Now, deadline.CheckTimeAt(Sample.Now));
        Assert.Equal(TimeSpan.FromMinutes(5), deadline.OverdueAt(Sample.Now));
    }

    [Fact]
    public void DeadlineNeedsAPointInTime()
    {
        Assert.Throws<DomainException>(() => AcknowledgementDeadline.At(default));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void OnCallTargetNeedsAName(string? name)
    {
        Assert.Throws<DomainException>(() => OnCallTarget.Named(name));
    }

    [Theory]
    [InlineData(1, "ana.silva")]
    [InlineData(2, "bruno.costa")]
    [InlineData(3, "carla.mendes")]
    public void RotationResolvesTheTargetForEachLevel(int level, string target)
    {
        Assert.Equal(OnCallTarget.Named(target), Sample.Rotation().TargetFor(EscalationLevel.From(level)));
    }

    [Fact]
    public void BreachReasonNamesTheLevel()
    {
        Assert.Equal("Acknowledgement SLA breached at level 2", EscalationReason.AcknowledgementBreached(EscalationLevel.Secondary).Text);
    }
}
