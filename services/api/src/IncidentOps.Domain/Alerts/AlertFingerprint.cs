using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Alerts;

public sealed record AlertFingerprint
{
    public const int MaxLength = 512;

    public AlertFingerprint(string? value)
    {
        Value = Guard.Required(value, "fingerprint", MaxLength);
    }

    public string Value { get; }

    public static AlertFingerprint FromAlertmanager(string? fingerprint) => new(fingerprint);

    public static AlertFingerprint FromAzureMonitor(string? alertRule, IReadOnlyList<string>? alertTargetIds)
    {
        var rule = Guard.Required(alertRule, "alertRule", MaxLength);
        return new AlertFingerprint($"{rule}|{FirstTarget(alertTargetIds)}");
    }

    public override string ToString() => Value;

    private static string FirstTarget(IReadOnlyList<string>? alertTargetIds)
    {
        if (alertTargetIds is null || alertTargetIds.Count == 0)
        {
            return string.Empty;
        }

        return alertTargetIds[0].Trim();
    }
}
