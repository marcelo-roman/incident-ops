using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Incidents;

public sealed record Description
{
    public const int MaxLength = 4000;

    public Description(string? value)
    {
        Value = Guard.Optional(value, "description", MaxLength);
    }

    public string Value { get; }

    public override string ToString() => Value;
}
