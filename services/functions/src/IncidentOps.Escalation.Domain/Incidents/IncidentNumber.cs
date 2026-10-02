using System.Globalization;

namespace IncidentOps.Escalation.Domain.Incidents;

public sealed record IncidentNumber
{
    private IncidentNumber(int value) => Value = value;

    public int Value { get; }

    public static IncidentNumber From(int value)
    {
        if (value < 1)
        {
            throw new DomainException($"An incident number must be positive, got {value}.");
        }

        return new IncidentNumber(value);
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"INC-{Value}");
}
