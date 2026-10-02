using IncidentOps.Escalation.Application.Messaging;

namespace IncidentOps.Functions.Tests.Application;

internal static class Messages
{
    public static InboundMessage Any() => new("message-1", ReadOnlyMemory<byte>.Empty, Sample.Now);
}
