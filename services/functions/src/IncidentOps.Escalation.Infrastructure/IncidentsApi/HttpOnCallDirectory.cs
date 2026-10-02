using System.Net.Http.Json;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Infrastructure.AntiCorruption;

namespace IncidentOps.Escalation.Infrastructure.IncidentsApi;

public sealed class HttpOnCallDirectory(HttpClient client) : IOnCallDirectory
{
    public async Task<OnCallRotation> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var onCall = await client.GetFromJsonAsync<OnCallDto>(IncidentsApiRoutes.CurrentOnCall, WireJson.Options, cancellationToken)
            ?? throw new UpstreamContractException("The incidents API returned an empty on-call roster.");
        return OnCallTranslator.ToRotation(onCall);
    }
}
