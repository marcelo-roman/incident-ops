using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Escalation.Application.Ports;

public interface ISlaCheckScheduler
{
    Task ScheduleAsync(ScheduledCheck check, CancellationToken cancellationToken);
}
