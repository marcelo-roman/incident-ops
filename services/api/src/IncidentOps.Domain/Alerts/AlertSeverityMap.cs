using IncidentOps.Domain.Incidents;

namespace IncidentOps.Domain.Alerts;

public static class AlertSeverityMap
{
    private static readonly Dictionary<string, Severity> AlertmanagerLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["critical"] = Severity.Sev1,
        ["high"] = Severity.Sev2,
        ["error"] = Severity.Sev2,
        ["warning"] = Severity.Sev3,
        ["info"] = Severity.Sev4,
    };

    private static readonly Dictionary<string, Severity> AzureMonitorSeverities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sev0"] = Severity.Sev1,
        ["Sev1"] = Severity.Sev1,
        ["Sev2"] = Severity.Sev2,
        ["Sev3"] = Severity.Sev3,
        ["Sev4"] = Severity.Sev4,
    };

    public static Severity FromAlertmanager(string? severityLabel) => Lookup(AlertmanagerLabels, severityLabel);

    public static Severity FromAzureMonitor(string? severity) => Lookup(AzureMonitorSeverities, severity);

    private static Severity Lookup(Dictionary<string, Severity> table, string? value)
    {
        if (value is not null && table.TryGetValue(value.Trim(), out var severity))
        {
            return severity;
        }

        return Severity.Sev4;
    }
}
