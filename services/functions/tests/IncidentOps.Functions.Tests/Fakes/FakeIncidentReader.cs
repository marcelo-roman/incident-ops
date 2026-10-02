using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Incidents;

namespace IncidentOps.Functions.Tests.Fakes;

internal sealed class FakeIncidentReader(IncidentState? state) : IIncidentReader
{
    public Task<IncidentState?> FindAsync(IncidentId incidentId, CancellationToken cancellationToken)
    {
        if (state is null || state.Id != incidentId)
        {
            return Task.FromResult<IncidentState?>(null);
        }

        return Task.FromResult<IncidentState?>(state);
    }
}
