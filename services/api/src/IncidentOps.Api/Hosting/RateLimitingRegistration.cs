using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace IncidentOps.Api.Hosting;

internal static class RateLimitingRegistration
{
    public const string WritesPolicy = "writes";
    public const string TokenPolicy = "token";

    public static IServiceCollection AddClientRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
        return services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = RejectAsync;
            limiter.AddPolicy(WritesPolicy, context => PerClientWindow(context, options.WritePermitLimit, options.WindowSeconds));
            limiter.AddPolicy(TokenPolicy, context => PerClientWindow(context, options.TokenPermitLimit, options.WindowSeconds));
        });
    }

    private static RateLimitPartition<string> PerClientWindow(HttpContext context, int permitLimit, int windowSeconds) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
            });

    private static async ValueTask RejectAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var problem = Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too many requests",
            detail: "The rate limit for this client was exceeded. Retry later.");

        await problem.ExecuteAsync(context.HttpContext);
    }
}
