namespace IncidentOps.Application.Common.Outbox;

public sealed record OutboxEntry(Guid Id, string Type, string Payload, DateTimeOffset OccurredAt);
