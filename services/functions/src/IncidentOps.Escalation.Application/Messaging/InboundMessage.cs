namespace IncidentOps.Escalation.Application.Messaging;

public sealed record InboundMessage(string MessageId, ReadOnlyMemory<byte> Body, DateTimeOffset ScheduledFor);
