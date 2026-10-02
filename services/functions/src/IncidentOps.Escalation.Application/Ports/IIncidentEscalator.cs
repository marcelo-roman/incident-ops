using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;

namespace IncidentOps.Escalation.Application.Ports;

public interface IIncidentEscalator
{
    Task<EscalationAttempt> EscalateAsync(IncidentId incidentId, EscalationReason reason, CancellationToken cancellationToken);
}
