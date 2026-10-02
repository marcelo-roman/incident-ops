using System.Net;
using System.Net.Http.Json;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Escalation.Infrastructure.AntiCorruption;

namespace IncidentOps.Escalation.Infrastructure.IncidentsApi;

public sealed class HttpIncidentEscalator(HttpClient client) : IIncidentEscalator
{
    public async Task<EscalationAttempt> EscalateAsync(IncidentId incidentId, EscalationReason reason, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            IncidentsApiRoutes.Escalate(incidentId),
            new EscalationRequest(reason.Text),
            WireJson.Options,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return EscalationAttempt.NotFound;
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return EscalationAttempt.Rejected;
        }

        response.EnsureSuccessStatusCode();
        return EscalationAttempt.Escalated;
    }
}
