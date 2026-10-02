using IncidentOps.Escalation.Domain.Escalation;

namespace IncidentOps.Escalation.Domain.Watches;

public abstract record EscalationDecision
{
    private EscalationDecision(string description) => Description = description;

    public string Description { get; }

    public sealed record Escalate : EscalationDecision
    {
        internal Escalate(EscalationReason reason, TimeSpan overdue)
            : base(reason.Text)
        {
            Reason = reason;
            Overdue = overdue;
        }

        public EscalationReason Reason { get; }

        public TimeSpan Overdue { get; }
    }

    public sealed record AlreadyAcknowledged : EscalationDecision
    {
        internal AlreadyAcknowledged()
            : base("incident already acknowledged")
        {
        }
    }

    public sealed record Superseded : EscalationDecision
    {
        internal Superseded(EscalationLevel watched, EscalationLevel current)
            : base($"watch for level {watched} superseded by level {current}")
        {
        }
    }

    public sealed record FinalLevelReached : EscalationDecision
    {
        internal FinalLevelReached()
            : base("final escalation level reached")
        {
        }
    }
}
