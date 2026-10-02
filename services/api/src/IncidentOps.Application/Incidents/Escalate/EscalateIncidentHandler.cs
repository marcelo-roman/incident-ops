using IncidentOps.Application.Common;
using IncidentOps.Application.Sla;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.OnCall;

namespace IncidentOps.Application.Incidents.Escalate;

public sealed class EscalateIncidentHandler(
    IIncidentRepository incidents,
    IOnCallRotationRepository rotations,
    SlaTargetsResolver sla,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<IncidentView> HandleAsync(Guid incidentId, EscalateIncident command, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetRequiredAsync(incidentId, cancellationToken);
        var rotation = await rotations.GetAsync(cancellationToken);
        var now = clock.UtcNow;

        incident.Escalate(new Note(command.Reason, "reason"), rotation.ShiftAt(now), sla.For(incident.Severity), now);

        await unitOfWork.CommitAsync(cancellationToken);
        return IncidentViews.From(incident, now);
    }
}
