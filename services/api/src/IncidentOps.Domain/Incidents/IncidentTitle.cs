using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Incidents;

public sealed record IncidentTitle
{
    public const int MaxLength = 200;

    public IncidentTitle(string? value)
    {
        Value = Guard.Required(value, "title", MaxLength);
    }

    public string Value { get; }

    public override string ToString() => Value;
}
