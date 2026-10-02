using System.Reflection;
using System.Runtime.CompilerServices;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Common;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.OnCall;
using NetArchTest.Rules;

namespace IncidentOps.Architecture.Tests;

public class DomainModelTests
{
    private static readonly Type[] DomainTypes = Layers.DomainAssembly.GetTypes();

    [Fact]
    public void Entities_have_no_public_setters()
    {
        var setters = DomainTypes
            .Where(type => type.IsClass && !IsRecord(type))
            .SelectMany(PublicProperties)
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(Describe);

        setters.Should().BeEmpty();
    }

    [Fact]
    public void Value_objects_are_immutable()
    {
        var mutable = DomainTypes
            .Where(IsRecord)
            .SelectMany(PublicProperties)
            .Where(property => property.SetMethod is { IsPublic: true } setter && !IsInitOnly(setter))
            .Select(Describe);

        mutable.Should().BeEmpty();
    }

    [Theory]
    [InlineData(typeof(Incident))]
    [InlineData(typeof(Service))]
    [InlineData(typeof(OnCallRotation))]
    public void Aggregates_derive_from_the_aggregate_root(Type aggregate)
    {
        typeof(IAggregateRoot).IsAssignableFrom(aggregate).Should().BeTrue();
    }

    [Fact]
    public void Timeline_entries_are_entities_owned_by_the_incident_aggregate()
    {
        typeof(IAggregateRoot).IsAssignableFrom(typeof(TimelineEntry)).Should().BeFalse();
        typeof(TimelineEntry).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Should().BeEmpty();
    }

    [Fact]
    public void Repositories_are_domain_ports_for_aggregate_roots()
    {
        var repositories = Types.InAssemblies([Layers.DomainAssembly, Layers.ApplicationAssembly])
            .That()
            .HaveNameEndingWith("Repository")
            .GetTypes()
            .ToList();

        repositories.Should().NotBeEmpty();
        repositories.Should().OnlyContain(type => type.IsInterface && type.Assembly == Layers.DomainAssembly);
    }

    [Fact]
    public void Domain_events_are_immutable_records()
    {
        var events = DomainTypes.Where(type => typeof(IDomainEvent).IsAssignableFrom(type) && !type.IsInterface).ToList();

        events.Should().HaveCountGreaterThanOrEqualTo(7);
        events.Should().OnlyContain(type => IsRecord(type));
    }

    private static IEnumerable<PropertyInfo> PublicProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    private static bool IsInitOnly(MethodInfo setter) =>
        setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$") is not null || type.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    private static string Describe(PropertyInfo property) => $"{property.DeclaringType!.Name}.{property.Name}";
}
