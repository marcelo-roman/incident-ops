using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;

namespace IncidentOps.Escalation.Domain.Watches;

public sealed record AcknowledgementWindowOpened
{
    public AcknowledgementWindowOpened(
        IncidentProfile incident,
        EscalationLevel level,
        AcknowledgementDeadline deadline,
        AcknowledgementState acknowledgement)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(deadline);
        Incident = incident;
        Level = level;
        Deadline = deadline;
        Acknowledgement = acknowledgement;
    }

    public IncidentProfile Incident { get; }

    public EscalationLevel Level { get; }

    public AcknowledgementDeadline Deadline { get; }

    public AcknowledgementState Acknowledgement { get; }

    public bool IsAwaitingAcknowledgement => Acknowledgement == AcknowledgementState.Pending;
}
