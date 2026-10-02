using System.Net.Http.Json;
using System.Text.Json.Nodes;
using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Api.Tests.Infrastructure;

public static class ApiClientExtensions
{
    public static async Task<IncidentView> TriggerAsync(
        this HttpClient client,
        Severity severity = Severity.Sev2,
        string serviceId = "checkout",
        string title = "Checkout latency above objective")
    {
        var response = await client.PostAsJsonAsync(
            "/api/incidents",
            new { title, description = "p95 above 2s", serviceId, severity = severity.ToString() });

        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<IncidentView>();
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(ContractJson.Options))!;

    public static async Task<JsonNode> ReadNodeAsync(this HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

    public static HttpRequestMessage WithApiKey(this HttpRequestMessage request)
    {
        request.Headers.Add("X-Api-Key", ApiFactory.ApiKey);
        return request;
    }

    public static HttpRequestMessage Post(string path, object body) =>
        new(HttpMethod.Post, path) { Content = JsonContent.Create(body) };

    public static HttpRequestMessage PostFixture(string path, string fixture, string run)
    {
        var payload = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture));
        foreach (var token in FixtureIdentities)
        {
            payload = payload.Replace(token, $"{token}-{run}", StringComparison.Ordinal);
        }

        return new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private static readonly string[] FixtureIdentities =
    [
        "c8a0f7d1e4b2a913",
        "5b1e9c0a7d3f2e64",
        "ca-incident-ops-api-failed-requests",
    ];
}
