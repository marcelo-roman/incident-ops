using System.Globalization;

namespace IncidentOps.Escalation.Domain.Escalation;

public sealed record EscalationReason
{
    private EscalationReason(string text) => Text = text;

    public string Text { get; }

    public static EscalationReason AcknowledgementBreached(EscalationLevel level) =>
        new(string.Create(CultureInfo.InvariantCulture, $"Acknowledgement SLA breached at level {level.Value}"));

    public override string ToString() => Text;
}
