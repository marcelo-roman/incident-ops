namespace IncidentOps.Api.Authentication;

public sealed record TokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt)
{
    public const string BearerType = "Bearer";
}
