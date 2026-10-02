using IncidentOps.Escalation.Domain.Paging;

namespace IncidentOps.Escalation.Application.Ports;

public interface IPager
{
    Task PageAsync(PageRequest request, CancellationToken cancellationToken);
}
