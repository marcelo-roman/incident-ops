namespace IncidentOps.Escalation.Domain.Incidents;

public sealed record IncidentId
{
    private IncidentId(Guid value) => Value = value;

    public Guid Value { get; }

    public static IncidentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("An incident id cannot be empty.");
        }

        return new IncidentId(value);
    }

    public override string ToString() => Value.ToString();
}
