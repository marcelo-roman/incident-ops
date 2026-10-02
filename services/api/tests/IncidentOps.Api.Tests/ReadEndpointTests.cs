using System.Net;
using IncidentOps.Api.Tests.Infrastructure;
using IncidentOps.Application.Catalog;
using IncidentOps.Application.Incidents;
using IncidentOps.Application.Metrics;
using IncidentOps.Application.OnCall;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Api.Tests;

[Collection(ApiFixtureGroup.Name)]
public class ReadEndpointTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Services_are_seeded()
    {
        var services = await (await _client.GetAsync("/api/services")).ReadAsync<List<ServiceView>>();

        services.Select(service => service.Id).Should().BeEquivalentTo(
            "checkout", "payments-gateway", "identity", "notifications", "search", "reporting", "platform");
    }

    [Fact]
    public async Task On_call_names_primary_secondary_and_lead()
    {
        var onCall = await (await _client.GetAsync("/api/oncall/current")).ReadAsync<OnCallView>();

        onCall.Primary.Should().NotBe(onCall.Secondary);
        onCall.Lead.Should().Be("Rachel Kim");
        onCall.WeekStart.DayOfWeek.Should().Be(DayOfWeek.Monday);
        onCall.WeekStart.Should().BeBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task History_covers_six_months()
    {
        var to = DateTimeOffset.UtcNow;
        var response = await _client.GetAsync($"/api/incidents/export?from={to.AddDays(-182):O}&to={to:O}".Replace("+", "%2B", StringComparison.Ordinal));
        var body = await response.ReadNodeAsync();
        var incidents = await response.ReadAsync<List<IncidentView>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        incidents.Count.Should().BeGreaterThan(300);
        incidents.Select(incident => incident.Title).Distinct().Count().Should().BeGreaterThan(40);
        incidents.Should().Contain(incident => incident.EscalationLevel > 1);
        incidents.Should().Contain(incident => incident.SlaState == Domain.Sla.SlaState.Breached);
        body[0]!.AsObject().ContainsKey("timeline").Should().BeFalse();
    }

    [Fact]
    public async Task Export_rejects_inverted_ranges()
    {
        var response = await _client.GetAsync("/api/incidents/export?from=2026-02-01T00:00:00Z&to=2026-01-01T00:00:00Z");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_filters_by_severity_and_open_newest_first()
    {
        await _client.TriggerAsync(Severity.Sev4, "search");

        var incidents = await (await _client.GetAsync("/api/incidents?severity=Sev4&open=true&limit=20")).ReadAsync<List<IncidentView>>();

        incidents.Should().NotBeEmpty().And.OnlyContain(incident => incident.Severity == Severity.Sev4 && incident.Status != IncidentStatus.Resolved);
        incidents.Should().BeInDescendingOrder(incident => incident.CreatedAt);
    }

    [Fact]
    public async Task Metrics_summary_has_every_severity()
    {
        var summary = await (await _client.GetAsync("/api/metrics/summary")).ReadAsync<MetricsSummaryView>();

        summary.OpenBySeverity.Keys.Should().BeEquivalentTo(Enum.GetValues<Severity>());
        summary.SlaCompliance30d.Should().BeInRange(0, 100);
        summary.Mtta30dMinutes.Should().BePositive();
        summary.Mttr30dMinutes.Should().BeGreaterThan(summary.Mtta30dMinutes);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_checks_are_healthy(string path)
    {
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Prometheus_endpoint_exposes_http_and_incident_metrics()
    {
        await _client.GetAsync("/api/services");
        await Task.Delay(TimeSpan.FromSeconds(2));

        var metrics = await _client.GetStringAsync("/metrics");

        metrics.Should().Contain("http_server_request_duration_seconds_bucket");
        metrics.Should().Contain("incidentops_incidents_open{");
        metrics.Should().Contain("incidentops_sla_breached_open");
    }

    [Fact]
    public async Task Swagger_document_is_served()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
