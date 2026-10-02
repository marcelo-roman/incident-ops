using System.Text.Json;
using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain;
using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Escalation.Infrastructure.AntiCorruption;

public sealed class SlaCheckMessageTranslator : IAcknowledgementCheckTranslator
{
    public AcknowledgementWatch Translate(InboundMessage message)
    {
        try
        {
            var check = JsonSerializer.Deserialize<SlaCheckMessage>(message.Body.Span, WireJson.Options)
                ?? throw new UpstreamContractException("The SLA check body is empty.");
            return AcknowledgementWatch.Resume(
                IncidentId.From(check.IncidentId),
                EscalationLevel.From(check.EscalationLevel),
                AcknowledgementDeadline.At(message.ScheduledFor));
        }
        catch (Exception exception) when (exception is JsonException or DomainException or UpstreamContractException)
        {
            throw new MalformedMessageException($"Message {message.MessageId} is not a valid SLA check: {exception.Message}", exception);
        }
    }

    public static SlaCheckMessage ToMessage(ScheduledCheck check) =>
        new(check.Key.IncidentId.Value, check.Key.Level.Value);
}
