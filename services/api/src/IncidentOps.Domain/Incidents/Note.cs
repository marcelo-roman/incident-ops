using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Incidents;

public sealed record Note
{
    public const int MaxLength = 2000;

    public Note(string? value)
        : this(value, "note")
    {
    }

    public Note(string? value, string field)
    {
        Value = Guard.Required(value, field, MaxLength);
    }

    public string Value { get; }

    public override string ToString() => Value;
}
