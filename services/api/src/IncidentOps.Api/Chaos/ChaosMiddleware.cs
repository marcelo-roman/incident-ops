using IncidentOps.Application.Common;

namespace IncidentOps.Api.Chaos;

internal sealed class ChaosMiddleware(RequestDelegate next, FaultSwitch faults, IClock clock)
{
    private static readonly PathString[] TargetedPaths = ["/api/incidents", "/api/services", "/api/oncall", "/api/metrics"];

    public async Task InvokeAsync(HttpContext context)
    {
        var fault = faults.ActiveAt(clock.UtcNow);
        if (fault is null || !IsTargeted(context.Request.Path))
        {
            await next(context);
            return;
        }

        await Task.Delay(fault.LatencyMs, context.RequestAborted);
        if (Random.Shared.NextDouble() >= fault.ErrorRate)
        {
            await next(context);
            return;
        }

        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Injected fault",
            detail: "Chaos fault injection is active.").ExecuteAsync(context);
    }

    private static bool IsTargeted(PathString path) =>
        TargetedPaths.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
}
