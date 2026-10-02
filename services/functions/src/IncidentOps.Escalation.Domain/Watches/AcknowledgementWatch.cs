using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;

namespace IncidentOps.Escalation.Domain.Watches;

public sealed class AcknowledgementWatch
{
    private AcknowledgementWatch(IncidentId incidentId, EscalationLevel level, AcknowledgementDeadline deadline)
    {
        IncidentId = incidentId;
        Level = level;
        Deadline = deadline;
    }

    public IncidentId IncidentId { get; }

    public EscalationLevel Level { get; }

    public AcknowledgementDeadline Deadline { get; }

    public WatchKey Key => new(IncidentId, Level);

    public static WatchOpening Open(AcknowledgementWindowOpened window)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (!window.IsAwaitingAcknowledgement)
        {
            return new WatchOpening.NotRequired("incident already acknowledged");
        }

        if (window.Level.IsFinal)
        {
            return new WatchOpening.NotRequired($"level {window.Level} is the final escalation level");
        }

        return new WatchOpening.Opened(new AcknowledgementWatch(window.Incident.Id, window.Level, window.Deadline));
    }

    public static AcknowledgementWatch Resume(IncidentId incidentId, EscalationLevel level, AcknowledgementDeadline deadline)
    {
        ArgumentNullException.ThrowIfNull(incidentId);
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(deadline);
        return new AcknowledgementWatch(incidentId, level, deadline);
    }

    public ScheduledCheck Schedule(DateTimeOffset now) => new(Key, Deadline.CheckTimeAt(now));

    public EscalationDecision Evaluate(IncidentState current, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (current.Id != IncidentId)
        {
            throw new DomainException($"Watch for incident {IncidentId} cannot evaluate incident {current.Id}.");
        }

        if (!current.IsAwaitingAcknowledgement)
        {
            return new EscalationDecision.AlreadyAcknowledged();
        }

        if (current.Level != Level)
        {
            return new EscalationDecision.Superseded(Level, current.Level);
        }

        if (Level.IsFinal)
        {
            return new EscalationDecision.FinalLevelReached();
        }

        return new EscalationDecision.Escalate(EscalationReason.AcknowledgementBreached(Level), Deadline.OverdueAt(now));
    }
}
