using System.Net;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Escalation.Infrastructure.AntiCorruption;
using IncidentOps.Functions.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentOps.Functions.Tests.Adapters;

public sealed class IncidentsApiClientTests
{
    private static string IncidentJson(string status, int level) => $$"""
        {"id":"{{Sample.IncidentGuid}}","number":1042,"title":"Checkout returns 502","serviceId":"checkout","severity":"Sev1",
         "status":"{{status}}","escalationLevel":{{level}},"ackDueAt":"2026-10-02T14:15:00Z","timeline":[]}
        """;

    [Fact]
    public async Task ReadsTheIncidentStateThroughTheAntiCorruptionLayer()
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.OK, IncidentJson("Acknowledged", 2));
        using var provider = ServiceProviders.WithHttpHandler(handler);

        var state = await provider.GetRequiredService<IIncidentReader>().FindAsync(Sample.IncidentId, CancellationToken.None);

        Assert.Equal(new IncidentState(Sample.IncidentId, EscalationLevel.Secondary, AcknowledgementState.Acknowledged), state);
        Assert.Equal(new Uri($"https://incidents-api.example.com/api/incidents/{Sample.IncidentGuid}"), Assert.Single(handler.Requests).Uri);
    }

    [Fact]
    public async Task ReturnsNothingForUnknownIncident()
    {
        using var provider = ServiceProviders.WithHttpHandler(new RecordingHttpHandler(HttpStatusCode.NotFound));

        Assert.Null(await provider.GetRequiredService<IIncidentReader>().FindAsync(Sample.IncidentId, CancellationToken.None));
    }

    [Fact]
    public async Task RejectsAnUpstreamStatusOutsideTheContract()
    {
        using var provider = ServiceProviders.WithHttpHandler(new RecordingHttpHandler(HttpStatusCode.OK, IncidentJson("Paused", 1)));

        await Assert.ThrowsAsync<UpstreamContractException>(() =>
            provider.GetRequiredService<IIncidentReader>().FindAsync(Sample.IncidentId, CancellationToken.None));
    }

    [Fact]
    public async Task EscalatesWithApiKeyAndReason()
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.OK, "{}");
        using var provider = ServiceProviders.WithHttpHandler(handler);

        var attempt = await provider.GetRequiredService<IIncidentEscalator>()
            .EscalateAsync(Sample.IncidentId, EscalationReason.AcknowledgementBreached(EscalationLevel.Primary), CancellationToken.None);

        Assert.Equal(EscalationAttempt.Escalated, attempt);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(new Uri($"https://incidents-api.example.com/api/incidents/{Sample.IncidentGuid}/escalate"), request.Uri);
        Assert.Equal("test-key", request.Headers["X-Api-Key"]);
        Assert.Equal("""{"reason":"Acknowledgement SLA breached at level 1"}""", request.Body);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, EscalationAttempt.Rejected)]
    [InlineData(HttpStatusCode.NotFound, EscalationAttempt.NotFound)]
    public async Task MapsEscalationResponses(HttpStatusCode statusCode, EscalationAttempt expected)
    {
        using var provider = ServiceProviders.WithHttpHandler(new RecordingHttpHandler(statusCode));

        var attempt = await provider.GetRequiredService<IIncidentEscalator>()
            .EscalateAsync(Sample.IncidentId, EscalationReason.AcknowledgementBreached(EscalationLevel.Primary), CancellationToken.None);

        Assert.Equal(expected, attempt);
    }

    [Fact]
    public async Task NeverRetriesTheEscalationPost()
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.ServiceUnavailable);
        using var provider = ServiceProviders.WithHttpHandler(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetRequiredService<IIncidentEscalator>()
            .EscalateAsync(Sample.IncidentId, EscalationReason.AcknowledgementBreached(EscalationLevel.Primary), CancellationToken.None));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ReadsTheRotationWithoutTheApiKey()
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.OK, """
            {"weekStart":"2026-09-28T14:30:00Z","primary":"ana.silva","secondary":"bruno.costa","lead":"carla.mendes"}
            """);
        using var provider = ServiceProviders.WithHttpHandler(handler);

        var rotation = await provider.GetRequiredService<IOnCallDirectory>().GetCurrentAsync(CancellationToken.None);

        Assert.Equal(Sample.Rotation(), rotation);
        Assert.False(Assert.Single(handler.Requests).Headers.ContainsKey("X-Api-Key"));
    }
}
