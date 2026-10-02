using System.Reflection;
using System.Runtime.CompilerServices;

namespace IncidentOps.Functions.Tests.Architecture;

public sealed class DomainImmutabilityTests
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    public static TheoryData<Type> DomainTypes()
    {
        var data = new TheoryData<Type>();
        foreach (var type in Layers.DomainAssembly.GetTypes().Where(IsModelType))
        {
            data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(DomainTypes))]
    public void DomainTypesHaveOnlyReadonlyState(Type type)
    {
        var writableFields = type.GetFields(Instance).Where(field => !field.IsInitOnly).Select(field => field.Name);

        Assert.Empty(writableFields);
    }

    [Theory]
    [MemberData(nameof(DomainTypes))]
    public void DomainTypesExposeNoSetters(Type type)
    {
        var setters = type.GetProperties(Instance)
            .Where(property => property.SetMethod is not null && !IsInitOnly(property.SetMethod))
            .Select(property => property.Name);

        Assert.Empty(setters);
    }

    private static bool IsModelType(Type type) =>
        type is { IsClass: true, Namespace: not null }
        && type.Namespace.StartsWith(Layers.Domain, StringComparison.Ordinal)
        && !typeof(Exception).IsAssignableFrom(type)
        && type.GetCustomAttribute<CompilerGeneratedAttribute>() is null;

    private static bool IsInitOnly(MethodInfo setter) =>
        setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
}
