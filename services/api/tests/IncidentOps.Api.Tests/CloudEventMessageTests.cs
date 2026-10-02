using System.Text.Json;
using IncidentOps.Application.Incidents;
using IncidentOps.Application.Incidents.Messaging;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Sla;
using IncidentOps.Infrastructure.Messaging;

namespace IncidentOps.Api.Tests;

public class CloudEventMessageTests
{
    [Fact]
    public void Message_is_a_structured_cloud_event_with_filter_properties()
    {
        var incident = new IncidentView
        {
            Id = Guid.NewGuid(),
            Number = 1042,
            Title = "Checkout errors",
            Description = string.Empty,
            ServiceId = "checkout",
            Severity = Severity.Sev1,
            Status = IncidentStatus.Triggered,
            Assignee = "Ava Thompson",
            EscalationLevel = 1,
            CreatedAt = new DateTimeOffset(2026, 10, 2, 14, 0, 0, TimeSpan.Zero),
            AcknowledgedAt = null,
            MitigatedAt = null,
            ResolvedAt = null,
            AckDueAt = new DateTimeOffset(2026, 10, 2, 14, 15, 0, TimeSpan.Zero),
            ResolveDueAt = new DateTimeOffset(2026, 10, 2, 18, 0, 0, TimeSpan.Zero),
            AcknowledgementBreached = false,
            SlaState = SlaState.OnTrack,
            RootCause = null,
            Source = IncidentSource.Manual,
            AlertFingerprint = null,
        };
        var incidentEvent = new IncidentIntegrationEvent(Guid.NewGuid(), IncidentEventTypes.Triggered, incident.CreatedAt, incident);

        var message = CloudEventMessageFactory.Create(incidentEvent);
        using var json = JsonDocument.Parse(message.Body.ToString());
        var root = json.RootElement;

        message.ContentType.Should().Be("application/cloudevents+json");
        message.MessageId.Should().Be(incidentEvent.Id.ToString());
        message.ApplicationProperties["eventType"].Should().Be("incident.triggered");
        message.ApplicationProperties["severity"].Should().Be("Sev1");
        root.GetProperty("specversion").GetString().Should().Be("1.0");
        root.GetProperty("type").GetString().Should().Be("incident.triggered");
        root.GetProperty("source").GetString().Should().Be("incident-ops-api");
        root.GetProperty("subject").GetString().Should().Be(incident.Id.ToString());
        root.GetProperty("time").GetString().Should().Be("2026-10-02T14:00:00.000Z");
        root.GetProperty("datacontenttype").GetString().Should().Be("application/json");
        root.GetProperty("data").GetProperty("number").GetInt32().Should().Be(1042);
        root.GetProperty("data").GetProperty("severity").GetString().Should().Be("Sev1");
        root.GetProperty("data").GetProperty("acknowledgementBreached").GetBoolean().Should().BeFalse();
    }
}
