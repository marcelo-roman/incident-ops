using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Functions.Tests.Fakes;

internal sealed class FakeAcknowledgementCheckTranslator(AcknowledgementWatch watch) : IAcknowledgementCheckTranslator
{
    public AcknowledgementWatch Translate(InboundMessage message) => watch;
}
