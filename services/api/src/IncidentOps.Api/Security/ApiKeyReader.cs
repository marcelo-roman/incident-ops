namespace IncidentOps.Api.Security;

internal static class ApiKeyReader
{
    public const string HeaderName = "X-Api-Key";
    public const string QueryName = "code";
    private const string BearerPrefix = "Bearer ";

    public static bool HasHeader(HttpRequest request) =>
        request.Headers.TryGetValue(HeaderName, out var header) && !string.IsNullOrEmpty(header);

    public static string? Read(HttpRequest request, ApiKeySources sources)
    {
        if (HasHeader(request))
        {
            return request.Headers[HeaderName].ToString();
        }

        var authorization = request.Headers.Authorization.ToString();
        if (sources.HasFlag(ApiKeySources.AuthorizationHeader) && authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[BearerPrefix.Length..].Trim();
        }

        if (!sources.HasFlag(ApiKeySources.Query))
        {
            return null;
        }

        return request.Query[QueryName].FirstOrDefault();
    }
}
