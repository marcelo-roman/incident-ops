using IncidentOps.Escalation.Domain.Escalation;

namespace IncidentOps.Escalation.Infrastructure.AntiCorruption;

public static class OnCallTranslator
{
    public static OnCallRotation ToRotation(OnCallDto dto) => new(
        OnCallTarget.Named(dto.Primary),
        OnCallTarget.Named(dto.Secondary),
        OnCallTarget.Named(dto.Lead));
}
