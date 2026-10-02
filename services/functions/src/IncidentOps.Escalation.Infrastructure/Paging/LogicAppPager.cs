using System.Net.Http.Headers;
using System.Text.Json;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Paging;
using IncidentOps.Escalation.Infrastructure.AntiCorruption;
using Microsoft.Extensions.Options;

namespace IncidentOps.Escalation.Infrastructure.Paging;

public sealed class LogicAppPager(HttpClient client, PagePayloadTranslator translator, IOptions<NotificationOptions> options) : IPager
{
    public async Task PageAsync(PageRequest request, CancellationToken cancellationToken)
    {
        using var content = BufferedJson(translator.ToPayload(request));
        using var response = await client.PostAsync(options.Value.LogicAppUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static ByteArrayContent BufferedJson(PagePayload payload)
    {
        var content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(payload, WireJson.Options));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        return content;
    }
}
