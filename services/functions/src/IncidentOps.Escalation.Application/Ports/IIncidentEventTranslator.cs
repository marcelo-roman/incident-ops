using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Escalation.Application.Ports;

public interface IIncidentEventTranslator
{
    Translation<AcknowledgementWindowOpened> Translate(InboundMessage message);
}
