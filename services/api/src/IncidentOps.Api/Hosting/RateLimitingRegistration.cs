using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace IncidentOps.Api.Hosting;

internal static class RateLimitingRegistration
{
    public const string WritesPolicy = "writes";

    public static IServiceCollection AddWriteRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
        return services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = WriteRejectionAsync;
            limiter.AddPolicy(WritesPolicy, context => PerClientWindow(context, options));
        });
    }

    private static RateLimitPartition<string> PerClientWindow(HttpContext context, RateLimitingOptions options) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.WritePermitLimit,
                Window = TimeSpan.FromSeconds(options.WindowSeconds),
                QueueLimit = 0,
            });

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var problem = Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too many requests",
            detail: "The write rate limit for this client was exceeded. Retry later.");

        await problem.ExecuteAsync(context.HttpContext);
    }
}
