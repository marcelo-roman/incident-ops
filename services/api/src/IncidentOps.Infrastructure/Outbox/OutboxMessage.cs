using IncidentOps.Application.Common.Outbox;

namespace IncidentOps.Infrastructure.Outbox;

internal sealed class OutboxMessage
{
    public const int ErrorMaxLength = 2000;

    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public long Sequence { get; }

    public string Type { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public DateTimeOffset? DeadLetteredAt { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage From(OutboxEntry entry) => new()
    {
        Id = entry.Id,
        Type = entry.Type,
        Payload = entry.Payload,
        OccurredAt = entry.OccurredAt,
        NextAttemptAt = entry.OccurredAt,
    };

    public void MarkProcessed(DateTimeOffset at)
    {
        Attempts++;
        ProcessedAt = at;
        LastError = null;
    }

    public void MarkFailed(string error, DateTimeOffset at, OutboxRetryPolicy retryPolicy)
    {
        Attempts++;
        LastError = error[..Math.Min(error.Length, ErrorMaxLength)];
        NextAttemptAt = at + retryPolicy.DelayAfter(Attempts);
        if (Attempts < retryPolicy.MaxAttempts)
        {
            return;
        }

        DeadLetteredAt = at;
    }
}
