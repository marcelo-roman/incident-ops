using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Escalation;

namespace IncidentOps.Functions.Tests.Fakes;

internal sealed class FakeOnCallDirectory(OnCallRotation rotation) : IOnCallDirectory
{
    public int Calls { get; private set; }

    public Task<OnCallRotation> GetCurrentAsync(CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(rotation);
    }
}
