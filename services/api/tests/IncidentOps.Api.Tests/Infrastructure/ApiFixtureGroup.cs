namespace IncidentOps.Api.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ApiFixtureGroup : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
