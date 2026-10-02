using IncidentOps.Domain.Common;
using IncidentOps.Domain.Errors;

namespace IncidentOps.Domain.Incidents;

public sealed record EscalationLevel
{
    public const int FirstValue = 1;
    public const int LastValue = 3;

    public static readonly EscalationLevel First = new(FirstValue);

    public EscalationLevel(int value)
    {
        Guard.Against(value is < FirstValue or > LastValue, "escalationLevel", $"Escalation levels go from {FirstValue} to {LastValue}.");
        Value = value;
    }

    public int Value { get; }

    public bool IsLast => Value == LastValue;

    public EscalationLevel Next()
    {
        if (IsLast)
        {
            throw new EscalationNotAllowedException($"Escalation stops at level {LastValue}.");
        }

        return new EscalationLevel(Value + 1);
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
