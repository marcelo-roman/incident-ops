using IncidentOps.Escalation.Domain.Escalation;

namespace IncidentOps.Escalation.Domain.Incidents;

public sealed record IncidentState
{
    public IncidentState(IncidentId id, EscalationLevel level, AcknowledgementState acknowledgement)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(level);
        Id = id;
        Level = level;
        Acknowledgement = acknowledgement;
    }

    public IncidentId Id { get; }

    public EscalationLevel Level { get; }

    public AcknowledgementState Acknowledgement { get; }

    public bool IsAwaitingAcknowledgement => Acknowledgement == AcknowledgementState.Pending;
}
