using IncidentOps.Escalation.Domain.Incidents;

namespace IncidentOps.Escalation.Application.Ports;

public interface IIncidentReader
{
    Task<IncidentState?> FindAsync(IncidentId incidentId, CancellationToken cancellationToken);
}
