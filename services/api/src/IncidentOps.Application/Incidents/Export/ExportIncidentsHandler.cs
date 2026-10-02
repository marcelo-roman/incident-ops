using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents.ReadModel;

namespace IncidentOps.Application.Incidents.Export;

public sealed class ExportIncidentsHandler(IIncidentQueries queries, IClock clock)
{
    public async Task<IReadOnlyList<IncidentView>> HandleAsync(ExportIncidents query, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var (from, to) = query.RangeEndingAt(now);
        var records = await queries.ListCreatedBetweenAsync(from, to, cancellationToken);
        return IncidentViews.From(records, now);
    }
}
