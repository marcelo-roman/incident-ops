namespace IncidentOps.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollingIntervalMilliseconds { get; set; } = 1000;

    public int BatchSize { get; set; } = 50;

    public int MaxAttempts { get; set; } = 20;

    public int MaxRetryDelaySeconds { get; set; } = 60;
}
