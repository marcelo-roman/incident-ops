using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents.ReadModel;

namespace IncidentOps.Application.Incidents.Details;

public sealed class GetIncidentHandler(IIncidentQueries queries, IClock clock)
{
    public async Task<IncidentDetailsView> HandleAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        var record = await queries.FindAsync(incidentId, cancellationToken)
            ?? throw new IncidentNotFoundException(incidentId);
        var timeline = await queries.TimelineAsync(incidentId, cancellationToken);

        return new IncidentDetailsView(IncidentViews.From(record, clock.UtcNow), timeline.Select(IncidentViews.From).ToList());
    }
}
