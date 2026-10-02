using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Incidents;

public sealed record RootCause
{
    public const int MaxLength = 2000;

    public RootCause(string? value)
    {
        Value = Guard.Required(value, "rootCause", MaxLength);
    }

    public string Value { get; }

    public override string ToString() => Value;
}
