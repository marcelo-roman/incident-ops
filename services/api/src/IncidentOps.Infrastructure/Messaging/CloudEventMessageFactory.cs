using System.Text.Json;
using Azure.Messaging.ServiceBus;
using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents.Messaging;

namespace IncidentOps.Infrastructure.Messaging;

public static class CloudEventMessageFactory
{
    public const string ContentType = "application/cloudevents+json";
    public const string Source = "incident-ops-api";
    public const string EventTypeProperty = "eventType";
    public const string SeverityProperty = "severity";

    public static ServiceBusMessage Create(IncidentIntegrationEvent incidentEvent)
    {
        var envelope = new CloudEventEnvelope(
            "1.0",
            incidentEvent.Id.ToString(),
            incidentEvent.Type,
            Source,
            incidentEvent.Time,
            incidentEvent.Incident.Id.ToString(),
            "application/json",
            incidentEvent.Incident);

        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(envelope, ContractJson.Options))
        {
            ContentType = ContentType,
            MessageId = envelope.Id,
            Subject = incidentEvent.Type,
            CorrelationId = envelope.Subject,
        };

        message.ApplicationProperties[EventTypeProperty] = incidentEvent.Type;
        message.ApplicationProperties[SeverityProperty] = incidentEvent.Incident.Severity.ToString();
        return message;
    }
}
