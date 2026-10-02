namespace IncidentOps.Api.Security;

internal static class ApiKeyEndpointExtensions
{
    public static TBuilder RequireApiKey<TBuilder>(this TBuilder builder, ApiKeySources sources = ApiKeySources.Header)
        where TBuilder : IEndpointConventionBuilder =>
        builder
            .WithMetadata(new ApiKeySourcesMetadata(sources))
            .RequireAuthorization(AuthorizationPolicies.ServiceApiKey);
}
