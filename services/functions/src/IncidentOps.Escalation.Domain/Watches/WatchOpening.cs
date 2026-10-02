namespace IncidentOps.Escalation.Domain.Watches;

public abstract record WatchOpening
{
    private WatchOpening(string description) => Description = description;

    public string Description { get; }

    public sealed record Opened : WatchOpening
    {
        internal Opened(AcknowledgementWatch watch)
            : base($"watching level {watch.Level} until {watch.Deadline.DueAt:O}") => Watch = watch;

        public AcknowledgementWatch Watch { get; }
    }

    public sealed record NotRequired : WatchOpening
    {
        internal NotRequired(string reason)
            : base(reason)
        {
        }
    }
}
