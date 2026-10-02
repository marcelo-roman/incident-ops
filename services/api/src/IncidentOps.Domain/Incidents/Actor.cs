using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Incidents;

public sealed record Actor
{
    public const int MaxLength = 100;

    public static readonly Actor System = new("system");

    public static readonly Actor Alerting = new("alerting");

    public Actor(string? value)
    {
        Value = Guard.Required(value, "actor", MaxLength);
    }

    public string Value { get; }

    public override string ToString() => Value;
}
