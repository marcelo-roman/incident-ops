namespace IncidentOps.Escalation.Domain.Incidents;

public sealed record ServiceId
{
    private ServiceId(string value) => Value = value;

    public string Value { get; }

    public static ServiceId From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("A service id cannot be blank.");
        }

        return new ServiceId(value.Trim());
    }

    public override string ToString() => Value;
}
