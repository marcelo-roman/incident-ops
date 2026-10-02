namespace IncidentOps.Infrastructure.Outbox;

internal sealed record OutboxRetryPolicy(int MaxAttempts, TimeSpan MaxDelay)
{
    public TimeSpan DelayAfter(int attempts)
    {
        var seconds = Math.Pow(2, Math.Min(attempts, 16));
        return TimeSpan.FromSeconds(Math.Min(seconds, MaxDelay.TotalSeconds));
    }
}
