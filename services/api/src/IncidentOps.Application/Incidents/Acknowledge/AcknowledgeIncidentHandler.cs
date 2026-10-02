using IncidentOps.Application.Common;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents.Acknowledge;

public sealed class AcknowledgeIncidentHandler(IIncidentRepository incidents, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<IncidentView> HandleAsync(Guid incidentId, AcknowledgeIncident command, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetRequiredAsync(incidentId, cancellationToken);
        var now = clock.UtcNow;

        incident.Acknowledge(new Actor(command.Actor), now);

        await unitOfWork.CommitAsync(cancellationToken);
        return IncidentViews.From(incident, now);
    }
}
