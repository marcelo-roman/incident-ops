using System.Net;
using System.Net.Http.Json;
using IncidentOps.Api.Tests.Infrastructure;
using IncidentOps.Application.Incidents;
using IncidentOps.Application.OnCall;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Api.Tests;

[Collection(ApiFixtureGroup.Name)]
public class EscalationEndpointTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Escalate_without_api_key_returns_401()
    {
        var incident = await _client.TriggerAsync(Severity.Sev1);

        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/escalate", new { reason = "No ack" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Escalate_with_wrong_api_key_returns_401()
    {
        var incident = await _client.TriggerAsync(Severity.Sev1);
        var request = ApiClientExtensions.Post($"/api/incidents/{incident.Id}/escalate", new { reason = "No ack" });
        request.Headers.Add("X-Api-Key", "wrong");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Escalation_walks_primary_secondary_lead_and_stops()
    {
        var incident = await _client.TriggerAsync(Severity.Sev1);
        var onCall = await (await _client.GetAsync("/api/oncall/current")).ReadAsync<OnCallView>();

        var second = await EscalateAsync(incident.Id);
        var third = await EscalateAsync(incident.Id);
        var fourth = await _client.SendAsync(Escalation(incident.Id));

        incident.Assignee.Should().Be(onCall.Primary);
        second.EscalationLevel.Should().Be(2);
        second.Assignee.Should().Be(onCall.Secondary);
        second.AckDueAt.Should().BeAfter(incident.AckDueAt);
        third.EscalationLevel.Should().Be(3);
        third.Assignee.Should().Be(onCall.Lead);
        fourth.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await factory.Events.WaitForTypesAsync(incident.Id, 3)).Should().Equal("incident.triggered", "incident.escalated", "incident.escalated");
    }

    [Fact]
    public async Task Acknowledged_incident_cannot_be_escalated()
    {
        var incident = await _client.TriggerAsync(Severity.Sev1);
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/acknowledge", new { actor = "Ava" });

        var response = await _client.SendAsync(Escalation(incident.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Escalated_then_resolved_incident_is_breached_and_excluded_from_compliance()
    {
        await using var compressed = factory.WithWebHostBuilder(builder => builder.UseSetting("Sla:TimeScale", "60"));
        var client = compressed.CreateClient();
        var incident = await client.TriggerAsync(Severity.Sev1, title: "Escalated twice then resolved");

        await SendAsync(client, Escalation(incident.Id));
        var escalated = await SendAsync(client, Escalation(incident.Id));
        await SendAsync(client, ApiClientExtensions.Post($"/api/incidents/{incident.Id}/acknowledge", new { actor = "Rachel Kim" }));
        await SendAsync(client, ApiClientExtensions.Post($"/api/incidents/{incident.Id}/mitigate", new { actor = "Rachel Kim", note = "Rolled back" }));
        var resolved = await SendAsync(client, ApiClientExtensions.Post($"/api/incidents/{incident.Id}/resolve", new { actor = "Rachel Kim", rootCause = "Bad deploy" }));
        var exported = await (await client.GetAsync("/api/incidents/export")).ReadAsync<List<IncidentView>>();

        escalated.EscalationLevel.Should().Be(3);
        resolved.AcknowledgedAt.Should().BeOnOrBefore(resolved.AckDueAt);
        resolved.ResolvedAt.Should().BeOnOrBefore(resolved.ResolveDueAt);
        resolved.AcknowledgementBreached.Should().BeTrue();
        resolved.SlaState.Should().Be(SlaState.Breached);
        exported.Single(item => item.Id == incident.Id).AcknowledgementBreached.Should().BeTrue();
    }

    private static async Task<IncidentView> SendAsync(HttpClient client, HttpRequestMessage request)
    {
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<IncidentView>();
    }

    private static HttpRequestMessage Escalation(Guid id) =>
        ApiClientExtensions.Post($"/api/incidents/{id}/escalate", new { reason = "Not acknowledged within SLA" }).WithApiKey();

    private async Task<IncidentView> EscalateAsync(Guid id)
    {
        var response = await _client.SendAsync(Escalation(id));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<IncidentView>();
    }
}
