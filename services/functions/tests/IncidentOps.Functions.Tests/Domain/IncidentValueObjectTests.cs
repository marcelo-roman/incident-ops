using IncidentOps.Escalation.Domain;
using IncidentOps.Escalation.Domain.Incidents;

namespace IncidentOps.Functions.Tests.Domain;

public sealed class IncidentValueObjectTests
{
    [Fact]
    public void IncidentIdRejectsEmptyGuid()
    {
        Assert.Throws<DomainException>(() => IncidentId.From(Guid.Empty));
    }

    [Fact]
    public void IncidentIdIsComparedByValue()
    {
        Assert.Equal(IncidentId.From(Sample.IncidentGuid), IncidentId.From(Sample.IncidentGuid));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IncidentNumberMustBePositive(int value)
    {
        Assert.Throws<DomainException>(() => IncidentNumber.From(value));
    }

    [Fact]
    public void IncidentNumberDisplaysAsTicket()
    {
        Assert.Equal("INC-1042", IncidentNumber.From(1042).ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ServiceIdRejectsBlank(string? value)
    {
        Assert.Throws<DomainException>(() => ServiceId.From(value));
    }

    [Theory]
    [InlineData("Sev1", true)]
    [InlineData("Sev2", true)]
    [InlineData("Sev3", false)]
    [InlineData("Sev4", false)]
    public void OnlySev1AndSev2PageOnCall(string name, bool pages)
    {
        Assert.Equal(pages, Severity.Parse(name).PagesOnCall);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("sev1")]
    [InlineData("Critical")]
    public void SeverityRejectsUnknownNames(string? name)
    {
        Assert.Throws<DomainException>(() => Severity.Parse(name));
    }

    [Fact]
    public void ProfileRequiresATitle()
    {
        Assert.Throws<DomainException>(() =>
            new IncidentProfile(Sample.IncidentId, IncidentNumber.From(1), " ", Severity.Sev1, ServiceId.From("checkout")));
    }
}
