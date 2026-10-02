using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents.ReadModel;

namespace IncidentOps.Application.Metrics;

public sealed class GetMetricsSummaryHandler(IIncidentQueries queries, IClock clock)
{
    public async Task<MetricsSummaryView> HandleAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var relevant = await queries.ListOpenOrCreatedSinceAsync(now - IncidentMetricsReport.Window, cancellationToken);
        return IncidentMetricsReport.Summarize([.. relevant], now);
    }
}
