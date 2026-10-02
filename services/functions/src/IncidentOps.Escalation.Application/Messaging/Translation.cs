namespace IncidentOps.Escalation.Application.Messaging;

public abstract record Translation<T>
{
    private Translation(string description) => Description = description;

    public string Description { get; }

    public sealed record Accepted : Translation<T>
    {
        public Accepted(T value)
            : base("accepted") => Value = value;

        public T Value { get; }
    }

    public sealed record Ignored : Translation<T>
    {
        public Ignored(string reason)
            : base(reason)
        {
        }
    }
}
