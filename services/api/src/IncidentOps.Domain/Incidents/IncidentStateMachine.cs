using IncidentOps.Domain.Errors;

namespace IncidentOps.Domain.Incidents;

public static class IncidentStateMachine
{
    private static readonly HashSet<(IncidentStatus From, IncidentStatus To)> AllowedTransitions =
    [
        (IncidentStatus.Triggered, IncidentStatus.Acknowledged),
        (IncidentStatus.Triggered, IncidentStatus.Mitigated),
        (IncidentStatus.Triggered, IncidentStatus.Resolved),
        (IncidentStatus.Acknowledged, IncidentStatus.Mitigated),
        (IncidentStatus.Acknowledged, IncidentStatus.Resolved),
        (IncidentStatus.Mitigated, IncidentStatus.Resolved),
    ];

    public static bool CanTransition(IncidentStatus from, IncidentStatus to) =>
        AllowedTransitions.Contains((from, to));

    public static void EnsureCanTransition(IncidentStatus from, IncidentStatus to)
    {
        if (CanTransition(from, to))
        {
            return;
        }

        throw new InvalidStatusTransitionException(from, to);
    }
}
