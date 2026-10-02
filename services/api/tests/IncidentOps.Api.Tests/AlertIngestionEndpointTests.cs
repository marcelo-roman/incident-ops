using System.Net;
using System.Net.Http.Headers;
using IncidentOps.Api.Tests.Infrastructure;
using IncidentOps.Application.Alerts;
using IncidentOps.Application.Incidents;
using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Api.Tests;

[Collection(ApiFixtureGroup.Name)]
public class AlertIngestionEndpointTests(ApiFactory factory)
{
    private const string Alertmanager = "/api/alerts/alertmanager";
    private const string AzureMonitor = "/api/alerts/azure-monitor";

    private readonly HttpClient _client = factory.CreateClient();
    private readonly HttpClient _alerting = factory.CreateAnonymousClient();
    private readonly string _run = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Alertmanager_lifecycle_opens_dedupes_and_mitigates()
    {
        var firing = await IngestAsync(ApiClientExtensions.PostFixture(Alertmanager, "alertmanager-firing.json", _run).WithApiKey());
        var repeated = await IngestAsync(ApiClientExtensions.PostFixture(Alertmanager, "alertmanager-firing.json", _run).WithApiKey());
        var resolved = await IngestAsync(ApiClientExtensions.PostFixture(Alertmanager, "alertmanager-resolved.json", _run).WithApiKey());

        firing.Alerts.Select(alert => alert.Action).Should().Equal(AlertAction.OpenIncident, AlertAction.OpenIncident);
        repeated.Alerts.Select(alert => alert.Action).Should().Equal(AlertAction.AppendToIncident, AlertAction.AppendToIncident);
        repeated.Alerts[0].IncidentId.Should().Be(firing.Alerts[0].IncidentId);
        resolved.Alerts.Should().ContainSingle().Which.IncidentId.Should().Be(firing.Alerts[0].IncidentId);

        var incident = await GetAsync(firing.Alerts[0].IncidentId!.Value);
        incident["source"]!.GetValue<string>().Should().Be("Alertmanager");
        incident["alertFingerprint"]!.GetValue<string>().Should().Be($"c8a0f7d1e4b2a913-{_run}");
        incident["title"]!.GetValue<string>().Should().Be("API 5xx error rate above 5% for 5 minutes");
        incident["serviceId"]!.GetValue<string>().Should().Be("checkout");
        incident["severity"]!.GetValue<string>().Should().Be("Sev1");
        incident["status"]!.GetValue<string>().Should().Be("Mitigated");
        incident["timeline"]!.AsArray().Select(entry => entry!["kind"]!.GetValue<string>())
            .Should().Equal("Triggered", "Alert", "Alert", "Mitigated");
        (await factory.Events.WaitForTypesAsync(firing.Alerts[0].IncidentId!.Value, 2)).Should().Equal("incident.triggered", "incident.mitigated");
    }

    [Fact]
    public async Task Alertmanager_unknown_service_falls_back_to_platform()
    {
        var result = await IngestAsync(ApiClientExtensions.PostFixture(Alertmanager, "alertmanager-firing.json", _run).WithApiKey());

        var incident = await GetAsync(result.Alerts[1].IncidentId!.Value);

        incident["serviceId"]!.GetValue<string>().Should().Be("platform");
        incident["severity"]!.GetValue<string>().Should().Be("Sev3");
    }

    [Fact]
    public async Task Azure_monitor_accepts_the_key_in_the_query_string()
    {
        var fired = await IngestAsync(ApiClientExtensions.PostFixture($"{AzureMonitor}?code={ApiFactory.ApiKey}", "azure-monitor-fired.json", _run));
        var resolved = await IngestAsync(ApiClientExtensions.PostFixture($"{AzureMonitor}?code={ApiFactory.ApiKey}", "azure-monitor-resolved.json", _run));

        var outcome = fired.Alerts.Should().ContainSingle().Subject;
        outcome.Action.Should().Be(AlertAction.OpenIncident);
        outcome.Fingerprint.Should().Be($"ca-incident-ops-api-failed-requests-{_run}|/subscriptions/00000000-0000-0000-0000-000000000000/resourcegroups/rg-incident-ops/providers/microsoft.insights/components/appi-incident-ops");
        resolved.Alerts.Single().Action.Should().Be(AlertAction.AppendToIncident);

        var incident = await (await _client.GetAsync($"/api/incidents/{outcome.IncidentId}")).ReadAsync<IncidentView>();
        incident.Source.Should().Be(IncidentSource.AzureMonitor);
        incident.ServiceId.Should().Be("identity");
        incident.Severity.Should().Be(Severity.Sev1);
        incident.Status.Should().Be(IncidentStatus.Mitigated);
    }

    [Fact]
    public async Task Bearer_token_is_accepted()
    {
        var request = ApiClientExtensions.PostFixture(Alertmanager, "alertmanager-resolved.json", _run);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiFactory.ApiKey);

        var response = await _alerting.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_key_returns_401()
    {
        var response = await _alerting.SendAsync(ApiClientExtensions.PostFixture(Alertmanager, "alertmanager-firing.json", _run));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Access_token_is_rejected()
    {
        var response = await _client.SendAsync(ApiClientExtensions.PostFixture(Alertmanager, "alertmanager-firing.json", _run));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unsupported_webhook_version_returns_400()
    {
        var request = ApiClientExtensions.Post(Alertmanager, new { version = "3", alerts = Array.Empty<object>() }).WithApiKey();

        var response = await _alerting.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<AlertIngestionResult> IngestAsync(HttpRequestMessage request)
    {
        var response = await _alerting.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<AlertIngestionResult>();
    }

    private async Task<System.Text.Json.Nodes.JsonNode> GetAsync(Guid id) =>
        await (await _client.GetAsync($"/api/incidents/{id}")).ReadNodeAsync();
}
