using Microsoft.AspNetCore.Authorization;

namespace IncidentOps.Api.Security;

internal static class AuthorizationPolicies
{
    public const string ServiceApiKey = "ServiceApiKey";

    public static AuthorizationPolicy AnyCaller { get; } = new AuthorizationPolicyBuilder(AuthenticationSchemes.BearerOrApiKey)
        .RequireAuthenticatedUser()
        .Build();

    public static AuthorizationPolicy ServiceOnly { get; } = new AuthorizationPolicyBuilder(AuthenticationSchemes.ApiKey)
        .RequireAuthenticatedUser()
        .Build();
}
