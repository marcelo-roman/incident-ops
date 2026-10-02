using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Functions.Tests.Fakes;

internal sealed class FakeSlaCheckScheduler : ISlaCheckScheduler
{
    public List<ScheduledCheck> Scheduled { get; } = [];

    public Task ScheduleAsync(ScheduledCheck check, CancellationToken cancellationToken)
    {
        Scheduled.Add(check);
        return Task.CompletedTask;
    }
}
