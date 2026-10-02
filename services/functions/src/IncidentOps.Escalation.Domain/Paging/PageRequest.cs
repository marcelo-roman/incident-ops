using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;

namespace IncidentOps.Escalation.Domain.Paging;

public sealed record PageRequest
{
    internal PageRequest(IncidentProfile incident, EscalationLevel level, OnCallTarget target)
    {
        Incident = incident;
        Level = level;
        Target = target;
    }

    public IncidentProfile Incident { get; }

    public EscalationLevel Level { get; }

    public OnCallTarget Target { get; }
}
