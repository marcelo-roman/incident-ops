namespace IncidentOps.Api.Security;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
