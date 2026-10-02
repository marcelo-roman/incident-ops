using IncidentOps.Application.Common;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents.Mitigate;

public sealed class MitigateIncidentHandler(IIncidentRepository incidents, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<IncidentView> HandleAsync(Guid incidentId, MitigateIncident command, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetRequiredAsync(incidentId, cancellationToken);
        var now = clock.UtcNow;

        incident.Mitigate(new Actor(command.Actor), new Note(command.Note), now);

        await unitOfWork.CommitAsync(cancellationToken);
        return IncidentViews.From(incident, now);
    }
}
