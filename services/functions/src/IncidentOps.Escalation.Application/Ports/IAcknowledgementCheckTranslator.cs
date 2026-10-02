using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Escalation.Application.Ports;

public interface IAcknowledgementCheckTranslator
{
    AcknowledgementWatch Translate(InboundMessage message);
}
