using System.Reflection;
using IncidentOps.Domain.Errors;
using NetArchTest.Rules;

namespace IncidentOps.Architecture.Tests;

public class ConventionTests
{
    [Fact]
    public void Application_has_one_sealed_handler_per_use_case()
    {
        var handlers = Types.InAssembly(Layers.ApplicationAssembly)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Handler")
            .GetTypes()
            .ToList();

        handlers.Should().HaveCountGreaterThanOrEqualTo(13);
        handlers.Should().OnlyContain(handler => handler.IsSealed);
        handlers.Should().OnlyContain(handler => HandleMethods(handler).Count() == 1);
    }

    [Fact]
    public void Infrastructure_implementations_are_internal_or_sealed()
    {
        var result = Types.InAssembly(Layers.InfrastructureAssembly)
            .That()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotResideInNamespace("IncidentOps.Infrastructure.Persistence.Migrations")

            .Should()
            .BeSealed()
            .Or()
            .BeStatic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_exceptions_share_a_base_type()
    {
        var result = Types.InAssembly(Layers.DomainAssembly)
            .That()
            .Inherit(typeof(Exception))
            .Should()
            .Inherit(typeof(DomainException))
            .Or()
            .HaveName(nameof(DomainException))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    private static IEnumerable<MethodInfo> HandleMethods(Type handler) =>
        handler.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == "HandleAsync");

}
