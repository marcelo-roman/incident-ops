using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Incidents;

public sealed class TimelineEntry : Entity<Guid>
{
    public const int MessageMaxLength = 4000;

    internal TimelineEntry(IncidentId incidentId, int sequence, TimelineKind kind, Actor actor, string message, DateTimeOffset at)
        : base(Guid.NewGuid())
    {
        IncidentId = incidentId;
        Sequence = sequence;
        Kind = kind;
        Actor = actor;
        Message = Text.Truncate(message, MessageMaxLength);
        At = at;
    }

    private TimelineEntry()
    {
    }

    public IncidentId IncidentId { get; private set; }

    public int Sequence { get; private set; }

    public DateTimeOffset At { get; private set; }

    public TimelineKind Kind { get; private set; }

    public Actor Actor { get; private set; } = Actor.System;

    public string Message { get; private set; } = string.Empty;
}
