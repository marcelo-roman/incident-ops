using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents.ReadModel;

namespace IncidentOps.Application.Incidents.Listing;

public sealed class ListIncidentsHandler(IIncidentQueries queries, IClock clock)
{
    public async Task<IReadOnlyList<IncidentView>> HandleAsync(ListIncidents query, CancellationToken cancellationToken)
    {
        var records = await queries.ListAsync(query.ToFilter(), cancellationToken);
        return IncidentViews.From(records, clock.UtcNow);
    }
}
