using IncidentOps.Domain.Incidents;

namespace IncidentOps.Domain.Sla;

public sealed record SlaClock
{
    public const double AtRiskRemainingRatio = 0.25;

    private SlaClock(
        DateTimeOffset startedAt,
        DateTimeOffset ackWindowStartsAt,
        DateTimeOffset ackDueAt,
        DateTimeOffset resolveDueAt,
        bool acknowledgementBreached)
    {
        StartedAt = startedAt;
        AckWindowStartsAt = ackWindowStartsAt;
        AckDueAt = ackDueAt;
        ResolveDueAt = resolveDueAt;
        AcknowledgementBreached = acknowledgementBreached;
    }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset AckWindowStartsAt { get; }

    public DateTimeOffset AckDueAt { get; }

    public DateTimeOffset ResolveDueAt { get; }

    public bool AcknowledgementBreached { get; }

    public static SlaClock Start(SlaTargets targets, DateTimeOffset at) =>
        new(at, at, at + targets.AcknowledgeWithin, at + targets.ResolveWithin, false);

    public static SlaClock Restore(
        DateTimeOffset startedAt,
        DateTimeOffset ackWindowStartsAt,
        DateTimeOffset ackDueAt,
        DateTimeOffset resolveDueAt,
        bool acknowledgementBreached) =>
        new(startedAt, ackWindowStartsAt, ackDueAt, resolveDueAt, acknowledgementBreached);

    public SlaClock RestartAcknowledgement(SlaTargets targets, DateTimeOffset at) =>
        new(StartedAt, at, at + targets.AcknowledgeWithin, ResolveDueAt, true);

    public SlaClock RecordAcknowledgement(DateTimeOffset at) =>
        new(StartedAt, AckWindowStartsAt, AckDueAt, ResolveDueAt, AcknowledgementBreached || at > AckDueAt);

    public bool Complies(DateTimeOffset? acknowledgedAt, DateTimeOffset? resolvedAt) =>
        acknowledgedAt is not null
        && !AcknowledgementBreached
        && resolvedAt is { } resolved
        && resolved <= ResolveDueAt;

    public SlaState StateAt(DateTimeOffset now, IncidentStatus status, DateTimeOffset? acknowledgedAt, DateTimeOffset? resolvedAt)
    {
        if (resolvedAt is not null)
        {
            return SettledState(acknowledgedAt, resolvedAt);
        }

        if (now > ResolveDueAt)
        {
            return SlaState.Breached;
        }

        return OpenState(now, status);
    }

    private SlaState SettledState(DateTimeOffset? acknowledgedAt, DateTimeOffset? resolvedAt)
    {
        if (Complies(acknowledgedAt, resolvedAt))
        {
            return SlaState.Met;
        }

        return SlaState.Breached;
    }

    private SlaState OpenState(DateTimeOffset now, IncidentStatus status)
    {
        var (startsAt, dueAt) = ActiveWindow(status);
        if (now > dueAt)
        {
            return SlaState.Breached;
        }

        if (dueAt - now < (dueAt - startsAt) * AtRiskRemainingRatio)
        {
            return SlaState.AtRisk;
        }

        return SlaState.OnTrack;
    }

    private (DateTimeOffset StartsAt, DateTimeOffset DueAt) ActiveWindow(IncidentStatus status)
    {
        if (status == IncidentStatus.Triggered)
        {
            return (AckWindowStartsAt, AckDueAt);
        }

        return (StartedAt, ResolveDueAt);
    }
}
