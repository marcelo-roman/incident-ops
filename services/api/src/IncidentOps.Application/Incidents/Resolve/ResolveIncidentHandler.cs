using IncidentOps.Application.Common;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents.Resolve;

public sealed class ResolveIncidentHandler(IIncidentRepository incidents, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<IncidentView> HandleAsync(Guid incidentId, ResolveIncident command, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetRequiredAsync(incidentId, cancellationToken);
        var now = clock.UtcNow;

        incident.Resolve(new Actor(command.Actor), new RootCause(command.RootCause), now);

        await unitOfWork.CommitAsync(cancellationToken);
        return IncidentViews.From(incident, now);
    }
}
