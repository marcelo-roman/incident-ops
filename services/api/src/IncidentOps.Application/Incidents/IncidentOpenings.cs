using IncidentOps.Application.Common;
using IncidentOps.Application.Sla;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.OnCall;

namespace IncidentOps.Application.Incidents;

public sealed class IncidentOpenings(
    IOnCallRotationRepository rotations,
    IIncidentRepository incidents,
    SlaTargetsResolver sla,
    IClock clock)
{
    public async Task<IncidentOpening> NextAsync(Severity severity, CancellationToken cancellationToken)
    {
        var targets = sla.For(severity);
        var now = clock.UtcNow;
        var rotation = await rotations.GetAsync(cancellationToken);
        var number = await incidents.NextNumberAsync(cancellationToken);
        return new IncidentOpening(number, rotation.ShiftAt(now), targets, now);
    }
}
