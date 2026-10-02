using NetArchTest.Rules;

namespace IncidentOps.Architecture.Tests;

public class LayerDependencyTests
{
    [Fact]
    public void Domain_depends_on_no_other_layer_or_framework()
    {
        var result = Types.InAssembly(Layers.DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny([Layers.Application, Layers.Infrastructure, Layers.Api, "Microsoft", .. Layers.Frameworks])
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Fact]
    public void Domain_references_only_the_base_class_library()
    {
        var references = Layers.DomainAssembly.GetReferencedAssemblies().Select(reference => reference.Name!);

        references.Should().OnlyContain(name => name.StartsWith("System", StringComparison.Ordinal) || name == "netstandard");
    }

    [Fact]
    public void Application_depends_only_on_domain()
    {
        var result = Types.InAssembly(Layers.ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny([Layers.Infrastructure, Layers.Api, .. Layers.Frameworks])
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_the_api()
    {
        var result = Types.InAssembly(Layers.InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(Layers.Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Theory]
    [InlineData("IncidentOps.Api.Incidents")]
    [InlineData("IncidentOps.Api.Alerts")]
    [InlineData("IncidentOps.Api.Catalog")]
    [InlineData("IncidentOps.Api.OnCall")]
    [InlineData("IncidentOps.Api.Metrics")]
    [InlineData("IncidentOps.Api.RealTime")]
    [InlineData("IncidentOps.Api.Chaos")]
    public void Api_features_depend_on_the_application_layer_only(string feature)
    {
        var result = Types.InAssembly(Layers.ApiAssembly)
            .That()
            .ResideInNamespace(feature)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.Domain, Layers.Infrastructure, "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Fact]
    public void Use_cases_reach_persistence_only_through_ports()
    {
        var result = Types.InAssembly(Layers.ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .ShouldNot()
            .HaveDependencyOnAny(Layers.Infrastructure, "Microsoft.EntityFrameworkCore", "System.Data")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    private static string Describe(TestResult result) =>
        $"these types break the rule: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
