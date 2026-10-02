using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Functions.Tests.Fakes;

internal sealed class FakeIncidentEventTranslator(Translation<AcknowledgementWindowOpened> translation) : IIncidentEventTranslator
{
    public static FakeIncidentEventTranslator Accepting(AcknowledgementWindowOpened window) =>
        new(new Translation<AcknowledgementWindowOpened>.Accepted(window));

    public static FakeIncidentEventTranslator Ignoring(string reason) =>
        new(new Translation<AcknowledgementWindowOpened>.Ignored(reason));

    public Translation<AcknowledgementWindowOpened> Translate(InboundMessage message) => translation;
}
