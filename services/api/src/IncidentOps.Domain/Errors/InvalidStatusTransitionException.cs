using IncidentOps.Domain.Incidents;

namespace IncidentOps.Domain.Errors;

public sealed class InvalidStatusTransitionException(IncidentStatus from, IncidentStatus to)
    : DomainException($"Cannot move an incident from {from} to {to}.")
{
    public IncidentStatus From { get; } = from;

    public IncidentStatus To { get; } = to;
}
