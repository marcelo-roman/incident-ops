using System.Text.Json;
using Azure.Messaging;
using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain;
using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Escalation.Infrastructure.AntiCorruption;

public sealed class CloudEventIncidentTranslator : IIncidentEventTranslator
{
    private static readonly string[] WindowOpeningTypes = ["incident.triggered", "incident.escalated"];

    public Translation<AcknowledgementWindowOpened> Translate(InboundMessage message)
    {
        try
        {
            return TranslateEnvelope(ParseEnvelope(message));
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or DomainException or UpstreamContractException)
        {
            throw new MalformedMessageException($"Message {message.MessageId} is not a valid incident CloudEvent: {exception.Message}", exception);
        }
    }

    private static CloudEvent ParseEnvelope(InboundMessage message) =>
        CloudEvent.Parse(new BinaryData(message.Body))
            ?? throw new UpstreamContractException("The message body is empty.");

    private static Translation<AcknowledgementWindowOpened> TranslateEnvelope(CloudEvent cloudEvent)
    {
        if (!WindowOpeningTypes.Contains(cloudEvent.Type))
        {
            return new Translation<AcknowledgementWindowOpened>.Ignored($"{cloudEvent.Type} does not open an acknowledgement window");
        }

        if (cloudEvent.Data is null)
        {
            throw new UpstreamContractException($"CloudEvent {cloudEvent.Id} has no data.");
        }

        var incident = cloudEvent.Data.ToObjectFromJson<IncidentDto>(WireJson.Options)
            ?? throw new UpstreamContractException($"CloudEvent {cloudEvent.Id} has empty data.");
        return new Translation<AcknowledgementWindowOpened>.Accepted(IncidentTranslator.ToWindow(incident));
    }
}
