using IncidentOps.Escalation.Domain.Escalation;

namespace IncidentOps.Escalation.Application.Ports;

public interface IOnCallDirectory
{
    Task<OnCallRotation> GetCurrentAsync(CancellationToken cancellationToken);
}
