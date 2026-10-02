using IncidentOps.Domain.Errors;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Domain.Sla;

public static class SlaPolicy
{
    private static readonly SlaTargets Sev1 = new(TimeSpan.FromMinutes(15), TimeSpan.FromHours(4));
    private static readonly SlaTargets Sev2 = new(TimeSpan.FromMinutes(30), TimeSpan.FromHours(8));
    private static readonly SlaTargets Sev3 = new(TimeSpan.FromHours(4), TimeSpan.FromDays(3));
    private static readonly SlaTargets Sev4 = new(TimeSpan.FromHours(24), TimeSpan.FromDays(10));

    public static SlaTargets For(Severity severity) => severity switch
    {
        Severity.Sev1 => Sev1,
        Severity.Sev2 => Sev2,
        Severity.Sev3 => Sev3,
        Severity.Sev4 => Sev4,
        _ => throw new DomainValidationException(nameof(severity), $"'{severity}' is not a known severity."),
    };
}
