namespace IncidentOps.Escalation.Domain.Escalation;

public sealed record OnCallRotation
{
    public OnCallRotation(OnCallTarget primary, OnCallTarget secondary, OnCallTarget lead)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(secondary);
        ArgumentNullException.ThrowIfNull(lead);
        Primary = primary;
        Secondary = secondary;
        Lead = lead;
    }

    public OnCallTarget Primary { get; }

    public OnCallTarget Secondary { get; }

    public OnCallTarget Lead { get; }

    public OnCallTarget TargetFor(EscalationLevel level)
    {
        if (level == EscalationLevel.Primary)
        {
            return Primary;
        }

        if (level == EscalationLevel.Secondary)
        {
            return Secondary;
        }

        return Lead;
    }
}
