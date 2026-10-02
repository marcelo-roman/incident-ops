using System.Net.Http.Json;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Paging;
using IncidentOps.Escalation.Infrastructure.AntiCorruption;
using Microsoft.Extensions.Options;

namespace IncidentOps.Escalation.Infrastructure.Paging;

public sealed class LogicAppPager(HttpClient client, PagePayloadTranslator translator, IOptions<NotificationOptions> options) : IPager
{
    public async Task PageAsync(PageRequest request, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            options.Value.LogicAppUrl,
            translator.ToPayload(request),
            WireJson.Options,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
