using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace IncidentOps.Api.Security;

internal sealed class ApiKeyEndpointFilter(IOptions<ApiKeyOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (Matches(options.Value.EscalationApiKey, ApiKeyReader.Read(context.HttpContext.Request)))
        {
            return await next(context);
        }

        return TypedResults.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            detail: $"A valid API key is required in '{ApiKeyReader.HeaderName}', 'Authorization: Bearer' or '?{ApiKeyReader.QueryName}='.");
    }

    private static bool Matches(string? expected, string? provided)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(provided))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(provided));
    }
}
