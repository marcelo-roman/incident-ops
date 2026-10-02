using IncidentOps.Api.Hosting;
using IncidentOps.Api.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IncidentOps.Api.Authentication;

internal static class TokenEndpoints
{
    public static RouteGroupBuilder MapTokenEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/auth/token", IssueToken)
            .WithName("IssueAccessToken")
            .WithTags("Authentication")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingRegistration.TokenPolicy)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        return api;
    }

    private static Results<Ok<TokenResponse>, ProblemHttpResult> IssueToken(
        TokenRequest request,
        DemoAccount account,
        AccessTokenIssuer issuer)
    {
        if (!account.Verify(request.Username, request.Password))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid username or password.");
        }

        var token = issuer.Issue(request.Username!);
        return TypedResults.Ok(new TokenResponse(token.Value, TokenResponse.BearerType, token.ExpiresAt));
    }
}
