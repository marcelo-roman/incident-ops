using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Escalation.Infrastructure.AntiCorruption;

public static class IncidentTranslator
{
    private const string Triggered = "Triggered";
    private static readonly string[] RespondedStatuses = ["Acknowledged", "Mitigated", "Resolved"];

    public static IncidentState ToState(IncidentDto dto) =>
        new(IncidentId.From(dto.Id), EscalationLevel.From(dto.EscalationLevel), AcknowledgementOf(dto.Status));

    public static AcknowledgementWindowOpened ToWindow(IncidentDto dto) => new(
        ToProfile(dto),
        EscalationLevel.From(dto.EscalationLevel),
        AcknowledgementDeadline.At(dto.AckDueAt),
        AcknowledgementOf(dto.Status));

    private static IncidentProfile ToProfile(IncidentDto dto) => new(
        IncidentId.From(dto.Id),
        IncidentNumber.From(dto.Number),
        dto.Title ?? string.Empty,
        Severity.Parse(dto.Severity),
        ServiceId.From(dto.ServiceId));

    private static AcknowledgementState AcknowledgementOf(string? status)
    {
        if (status == Triggered)
        {
            return AcknowledgementState.Pending;
        }

        if (RespondedStatuses.Contains(status))
        {
            return AcknowledgementState.Acknowledged;
        }

        throw new UpstreamContractException($"Unknown incident status '{status}'.");
    }
}
