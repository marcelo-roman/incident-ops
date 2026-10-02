using System.Globalization;

namespace IncidentOps.Escalation.Domain.Escalation;

public sealed record EscalationLevel
{
    private const int FirstValue = 1;
    private const int FinalValue = 3;

    public static readonly EscalationLevel Primary = new(FirstValue);
    public static readonly EscalationLevel Secondary = new(2);
    public static readonly EscalationLevel Lead = new(FinalValue);

    private EscalationLevel(int value) => Value = value;

    public int Value { get; }

    public bool IsFinal => Value == FinalValue;

    public static EscalationLevel From(int value)
    {
        if (value is < FirstValue or > FinalValue)
        {
            throw new DomainException($"Escalation level must be between {FirstValue} and {FinalValue}, got {value}.");
        }

        return new EscalationLevel(value);
    }

    public EscalationLevel Next()
    {
        if (IsFinal)
        {
            throw new DomainException($"Escalation level {Value} is final.");
        }

        return new EscalationLevel(Value + 1);
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
