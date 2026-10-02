using IncidentOps.Application.Metrics;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IncidentOps.Api.Metrics;

internal static class MetricsEndpoints
{
    public static RouteGroupBuilder MapMetricsEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/metrics/summary", GetSummary).WithName("GetMetricsSummary").WithTags("Metrics");
        return api;
    }

    private static async Task<Ok<MetricsSummaryView>> GetSummary(
        GetMetricsSummaryHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(cancellationToken));
}
