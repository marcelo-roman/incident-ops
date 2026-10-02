namespace IncidentOps.Escalation.Domain.Escalation;

public sealed record OnCallTarget
{
    private OnCallTarget(string name) => Name = name;

    public string Name { get; }

    public static OnCallTarget Named(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("An on-call target needs a name.");
        }

        return new OnCallTarget(name.Trim());
    }

    public override string ToString() => Name;
}
