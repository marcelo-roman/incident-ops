namespace IncidentOps.Escalation.Domain.Escalation;

public sealed record AcknowledgementDeadline
{
    private AcknowledgementDeadline(DateTimeOffset dueAt) => DueAt = dueAt;

    public DateTimeOffset DueAt { get; }

    public static AcknowledgementDeadline At(DateTimeOffset dueAt)
    {
        if (dueAt == default)
        {
            throw new DomainException("An acknowledgement deadline needs a point in time.");
        }

        return new AcknowledgementDeadline(dueAt.ToUniversalTime());
    }

    public bool HasPassedAt(DateTimeOffset now) => DueAt <= now;

    public DateTimeOffset CheckTimeAt(DateTimeOffset now)
    {
        if (HasPassedAt(now))
        {
            return now;
        }

        return DueAt;
    }

    public TimeSpan OverdueAt(DateTimeOffset now)
    {
        if (!HasPassedAt(now))
        {
            return TimeSpan.Zero;
        }

        return now - DueAt;
    }
}
