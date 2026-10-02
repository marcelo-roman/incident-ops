namespace IncidentOps.Api.Security;

internal static class ApiKeyReader
{
    public const string HeaderName = "X-Api-Key";
    public const string QueryName = "code";
    private const string BearerPrefix = "Bearer ";

    public static string? Read(HttpRequest request)
    {
        if (request.Headers.TryGetValue(HeaderName, out var header) && !string.IsNullOrEmpty(header))
        {
            return header.ToString();
        }

        var authorization = request.Headers.Authorization.ToString();
        if (authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[BearerPrefix.Length..].Trim();
        }

        return request.Query[QueryName].FirstOrDefault();
    }
}
