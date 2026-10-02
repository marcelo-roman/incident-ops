using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Incidents;

public sealed record IncidentNumber
{
    public const string Prefix = "INC-";

    public IncidentNumber(int value)
    {
        Guard.Against(value < 1, "number", "Incident numbers start at 1.");
        Value = value;
    }

    public int Value { get; }

    public override string ToString() => $"{Prefix}{Value}";
}
