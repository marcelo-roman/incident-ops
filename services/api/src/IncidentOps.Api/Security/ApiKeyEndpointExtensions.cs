namespace IncidentOps.Api.Security;

internal static class ApiKeyEndpointExtensions
{
    public static TBuilder RequireApiKey<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter<TBuilder, ApiKeyEndpointFilter>();
}
