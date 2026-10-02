using IncidentOps.Domain.Incidents;

namespace IncidentOps.Domain.OnCall;

public sealed record OnCallShift(DateTimeOffset WeekStart, Actor Primary, Actor Secondary, Actor Lead)
{
    public Actor TargetFor(EscalationLevel level) => level.Value switch
    {
        1 => Primary,
        2 => Secondary,
        _ => Lead,
    };
}
