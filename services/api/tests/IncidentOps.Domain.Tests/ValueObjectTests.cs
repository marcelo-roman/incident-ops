using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Errors;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Domain.Tests;

public class ValueObjectTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Title_is_required(string? value)
    {
        var act = () => new IncidentTitle(value);

        act.Should().Throw<DomainValidationException>().Which.Field.Should().Be("title");
    }

    [Fact]
    public void Title_is_bounded_and_trimmed()
    {
        new IncidentTitle("  Checkout down  ").Value.Should().Be("Checkout down");
        var act = () => new IncidentTitle(new string('x', IncidentTitle.MaxLength + 1));

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Description_is_optional()
    {
        new Description(null).Value.Should().BeEmpty();
    }

    [Theory]
    [InlineData("checkout")]
    [InlineData("payments-gateway")]
    public void Service_id_accepts_slugs(string value)
    {
        new ServiceId(value).Value.Should().Be(value);
    }

    [Theory]
    [InlineData("Checkout")]
    [InlineData("pay ments")]
    [InlineData("-checkout")]
    [InlineData("")]
    public void Service_id_rejects_non_slugs(string value)
    {
        var act = () => new ServiceId(value);

        act.Should().Throw<DomainValidationException>().Which.Field.Should().Be("serviceId");
    }

    [Fact]
    public void Value_objects_compare_by_value()
    {
        new ServiceId("checkout").Should().Be(new ServiceId("checkout"));
        new Actor("Ava").Should().Be(new Actor("Ava"));
        new AlertFingerprint("fp").Should().NotBe(new AlertFingerprint("other"));
    }

    [Fact]
    public void Incident_number_formats_as_inc()
    {
        new IncidentNumber(1042).ToString().Should().Be("INC-1042");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Incident_number_starts_at_one(int value)
    {
        var act = () => new IncidentNumber(value);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Escalation_level_walks_one_to_three()
    {
        var level = EscalationLevel.First;

        level.Next().Value.Should().Be(2);
        level.Next().Next().IsLast.Should().BeTrue();
    }

    [Fact]
    public void Escalation_level_stops_at_three()
    {
        var act = () => new EscalationLevel(3).Next();

        act.Should().Throw<EscalationNotAllowedException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Escalation_level_is_bounded(int value)
    {
        var act = () => new EscalationLevel(value);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Note_reports_the_field_it_validates()
    {
        var act = () => new Note(" ", "reason");

        act.Should().Throw<DomainValidationException>().Which.Field.Should().Be("reason");
    }

    [Fact]
    public void Actor_rejects_blank_names()
    {
        var act = () => new Actor("");

        act.Should().Throw<DomainValidationException>().Which.Field.Should().Be("actor");
    }

    [Fact]
    public void Incident_ids_are_unique()
    {
        IncidentId.New().Should().NotBe(IncidentId.New());
    }
}
