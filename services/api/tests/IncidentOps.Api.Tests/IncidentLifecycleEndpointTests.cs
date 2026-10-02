using System.Net;
using System.Net.Http.Json;
using IncidentOps.Api.Tests.Infrastructure;
using IncidentOps.Application.Incidents;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Api.Tests;

[Collection(ApiFixtureGroup.Name)]
public class IncidentLifecycleEndpointTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Trigger_returns_201_with_the_contract_shape()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/incidents",
            new { title = "Checkout errors", description = "5xx on payment step", serviceId = "checkout", severity = "Sev1" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.ReadNodeAsync();
        response.Headers.Location!.ToString().Should().Be($"/api/incidents/{body["id"]}");
        body["number"]!.GetValue<int>().Should().BeGreaterThan(1001);
        body["severity"]!.GetValue<string>().Should().Be("Sev1");
        body["status"]!.GetValue<string>().Should().Be("Triggered");
        body["slaState"]!.GetValue<string>().Should().Be("OnTrack");
        body["source"]!.GetValue<string>().Should().Be("Manual");
        body["escalationLevel"]!.GetValue<int>().Should().Be(1);
        body["assignee"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        body["createdAt"]!.GetValue<string>().Should().EndWith("Z");
        body["acknowledgementBreached"]!.GetValue<bool>().Should().BeFalse();
        body.AsObject().Should().ContainKeys("acknowledgedAt", "mitigatedAt", "resolvedAt", "rootCause", "alertFingerprint");
    }

    [Fact]
    public async Task Trigger_publishes_incident_triggered()
    {
        var incident = await _client.TriggerAsync(Severity.Sev2);

        (await factory.Events.WaitForTypesAsync(incident.Id, 1)).Should().Equal("incident.triggered");
        (incident.AckDueAt - incident.CreatedAt).Should().Be(TimeSpan.FromMinutes(30));
        (incident.ResolveDueAt - incident.CreatedAt).Should().Be(TimeSpan.FromHours(8));
    }

    [Fact]
    public async Task Full_lifecycle_reaches_resolved_and_met()
    {
        var incident = await _client.TriggerAsync();

        var acknowledged = await PostAsync($"/api/incidents/{incident.Id}/acknowledge", new { actor = "Ava Thompson" });
        var mitigated = await PostAsync($"/api/incidents/{incident.Id}/mitigate", new { actor = "Ava Thompson", note = "Rolled back" });
        var resolved = await PostAsync($"/api/incidents/{incident.Id}/resolve", new { actor = "Ava Thompson", rootCause = "Bad config" });

        acknowledged.Status.Should().Be(IncidentStatus.Acknowledged);
        acknowledged.Assignee.Should().Be("Ava Thompson");
        mitigated.Status.Should().Be(IncidentStatus.Mitigated);
        resolved.Status.Should().Be(IncidentStatus.Resolved);
        resolved.SlaState.Should().Be(SlaState.Met);
        resolved.RootCause.Should().Be("Bad config");
        (await factory.Events.WaitForTypesAsync(incident.Id, 4)).Should().Equal(
            "incident.triggered", "incident.acknowledged", "incident.mitigated", "incident.resolved");
    }

    [Fact]
    public async Task Invalid_transition_returns_409_problem()
    {
        var incident = await _client.TriggerAsync();
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/resolve", new { actor = "Ava", rootCause = "Duplicate" });

        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/acknowledge", new { actor = "Ava" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Unknown_service_returns_400_validation_problem()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/incidents",
            new { title = "x", description = "y", serviceId = "does-not-exist", severity = "Sev3" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadNodeAsync();
        problem["errors"]!["serviceId"].Should().NotBeNull();
    }

    [Fact]
    public async Task Missing_title_returns_400_validation_problem()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/incidents",
            new { title = "", description = "y", serviceId = "checkout", severity = "Sev3" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadNodeAsync())["errors"]!["title"].Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_incident_returns_404_problem()
    {
        var response = await _client.GetAsync($"/api/incidents/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Notes_are_returned_and_appear_in_the_timeline()
    {
        var incident = await _client.TriggerAsync();

        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/notes", new { actor = "Mei Tanaka", message = "Checking dashboards" });
        var note = await response.ReadAsync<TimelineEntryView>();
        var details = await (await _client.GetAsync($"/api/incidents/{incident.Id}")).ReadNodeAsync();

        note.Kind.Should().Be(TimelineKind.Note);
        note.IncidentId.Should().Be(incident.Id);
        details["timeline"]!.AsArray().Select(entry => entry!["kind"]!.GetValue<string>())
            .Should().Equal("Triggered", "Note");
        (await factory.Events.WaitForTypesAsync(incident.Id, 1)).Should().Equal("incident.triggered");
    }

    private async Task<IncidentView> PostAsync(string path, object body)
    {
        var response = await _client.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<IncidentView>();
    }
}
