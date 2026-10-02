using System.Net;
using System.Net.Http.Json;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Escalation.Infrastructure.AntiCorruption;

namespace IncidentOps.Escalation.Infrastructure.IncidentsApi;

public sealed class HttpIncidentReader(HttpClient client) : IIncidentReader
{
    public async Task<IncidentState?> FindAsync(IncidentId incidentId, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(IncidentsApiRoutes.Incident(incidentId), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var incident = await response.Content.ReadFromJsonAsync<IncidentDto>(WireJson.Options, cancellationToken)
            ?? throw new UpstreamContractException($"The incidents API returned an empty body for {incidentId}.");
        return IncidentTranslator.ToState(incident);
    }
}
