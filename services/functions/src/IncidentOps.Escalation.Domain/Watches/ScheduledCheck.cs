namespace IncidentOps.Escalation.Domain.Watches;

public sealed record ScheduledCheck
{
    public ScheduledCheck(WatchKey key, DateTimeOffset checkAt)
    {
        ArgumentNullException.ThrowIfNull(key);
        Key = key;
        CheckAt = checkAt;
    }

    public WatchKey Key { get; }

    public DateTimeOffset CheckAt { get; }
}
