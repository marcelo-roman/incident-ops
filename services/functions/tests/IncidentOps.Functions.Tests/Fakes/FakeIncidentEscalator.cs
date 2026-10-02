using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;

namespace IncidentOps.Functions.Tests.Fakes;

internal sealed class FakeIncidentEscalator(EscalationAttempt attempt = EscalationAttempt.Escalated) : IIncidentEscalator
{
    public List<(IncidentId IncidentId, EscalationReason Reason)> Calls { get; } = [];

    public Task<EscalationAttempt> EscalateAsync(IncidentId incidentId, EscalationReason reason, CancellationToken cancellationToken)
    {
        Calls.Add((incidentId, reason));
        return Task.FromResult(attempt);
    }
}
