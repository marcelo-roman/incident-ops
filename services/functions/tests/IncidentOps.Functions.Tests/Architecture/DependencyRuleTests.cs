using NetArchTest.Rules;

namespace IncidentOps.Functions.Tests.Architecture;

public sealed class DependencyRuleTests
{
    private const string CoverageInstrumentation = "Coverlet.Core.Instrumentation";

    private static readonly string[] Frameworks =
    [
        "Microsoft.Azure.Functions",
        "Azure.",
        "Microsoft.ApplicationInsights",
        "Microsoft.Extensions.Http",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Configuration",
        "Microsoft.Extensions.Options",
        "System.Net.Http",
        "System.Text.Json",
    ];

    [Fact]
    public void DomainDependsOnNothingOutsideItself()
    {
        AssertRule(
            Types.InAssembly(Layers.DomainAssembly).That().ResideInNamespaceStartingWith(Layers.Domain),
            types => types.ShouldNot().HaveDependencyOnAny(
                [Layers.Application, Layers.Infrastructure, Layers.Host + ".", "Microsoft.", .. Frameworks]));
    }

    [Fact]
    public void ApplicationDependsOnDomainAndAbstractionsOnly()
    {
        AssertRule(
            Types.InAssembly(Layers.ApplicationAssembly).That().ResideInNamespaceStartingWith(Layers.Application),
            types => types.ShouldNot().HaveDependencyOnAny([Layers.Infrastructure, Layers.Host + ".", .. Frameworks]));
    }

    [Fact]
    public void UseCasesDependOnDomainAndPortsOnly()
    {
        AssertRule(
            Types.InAssembly(Layers.ApplicationAssembly).That().ResideInNamespace(Layers.Application + ".UseCases"),
            types => types.Should().OnlyHaveDependenciesOn(
                Layers.Domain,
                Layers.Application + ".Ports",
                Layers.Application + ".Messaging",
                Layers.Application + ".UseCases",
                "Microsoft.Extensions.Logging",
                "System",
                CoverageInstrumentation));
    }

    [Fact]
    public void PortsAreTheOnlyApplicationInterfaces()
    {
        AssertRule(
            Types.InAssembly(Layers.ApplicationAssembly).That().AreInterfaces(),
            types => types.Should().ResideInNamespace(Layers.Application + ".Ports"));
    }

    [Fact]
    public void TriggersDependOnTheApplicationOnly()
    {
        AssertRule(
            Types.InAssembly(Layers.HostAssembly).That().ResideInNamespace(Layers.Host + ".Triggers"),
            types => types.ShouldNot().HaveDependencyOnAny(Layers.Domain, Layers.Infrastructure, "System.Net.Http", "Azure.Identity"));
    }

    [Fact]
    public void InfrastructureDoesNotReachIntoTheHost()
    {
        AssertRule(
            Types.InAssembly(Layers.InfrastructureAssembly).That().ResideInNamespaceStartingWith(Layers.Infrastructure),
            types => types.ShouldNot().HaveDependencyOn(Layers.Host + "."));
    }

    [Fact]
    public void UpstreamDtosStayInsideTheAntiCorruptionLayer()
    {
        AssertRule(
            Types.InAssembly(Layers.InfrastructureAssembly)
                .That()
                .ResideInNamespaceStartingWith(Layers.Infrastructure)
                .And()
                .DoNotResideInNamespace(Layers.Infrastructure + ".AntiCorruption")
                .And()
                .DoNotResideInNamespace(Layers.Infrastructure + ".IncidentsApi"),
            types => types.ShouldNot().HaveDependencyOnAny(
                Layers.Infrastructure + ".AntiCorruption.IncidentDto",
                Layers.Infrastructure + ".AntiCorruption.OnCallDto"));
    }

    [Theory]
    [InlineData(Layers.Domain)]
    [InlineData(Layers.Application)]
    [InlineData(Layers.Infrastructure)]
    [InlineData(Layers.Host + ".")]
    public void ConcreteTypesAreSealed(string layer)
    {
        var assembly = AssemblyOf(layer);
        AssertRule(
            Types.InAssembly(assembly).That().AreClasses().And().AreNotAbstract().And().ResideInNamespaceStartingWith(layer),
            types => types.Should().BeSealed());
    }

    private static System.Reflection.Assembly AssemblyOf(string layer) => layer switch
    {
        Layers.Domain => Layers.DomainAssembly,
        Layers.Application => Layers.ApplicationAssembly,
        Layers.Infrastructure => Layers.InfrastructureAssembly,
        _ => Layers.HostAssembly,
    };

    private static void AssertRule(PredicateList selection, Func<PredicateList, ConditionList> rule)
    {
        Assert.NotEmpty(selection.GetTypes());
        var result = rule(selection).GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
