using IncidentOps.Application.Common;
using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Alerts.AzureMonitor;

public static class AzureMonitorTranslator
{
    public const string CommonAlertSchema = "azureMonitorCommonAlertSchema";

    public static IReadOnlyList<AlertSignal> ToSignals(AzureMonitorAlert alert)
    {
        if (!string.Equals(alert.SchemaId, CommonAlertSchema, StringComparison.Ordinal))
        {
            throw new RequestValidationException("schemaId", $"Only the {CommonAlertSchema} payload is supported.");
        }

        var essentials = alert.Data?.Essentials
            ?? throw new RequestValidationException("data.essentials", "The alert has no essentials section.");

        return [ToSignal(essentials, alert.Data.CustomProperties)];
    }

    private static AlertSignal ToSignal(AzureMonitorEssentials essentials, IReadOnlyDictionary<string, string>? customProperties) => new(
        IncidentSource.AzureMonitor,
        AlertFingerprint.FromAzureMonitor(essentials.AlertRule, essentials.AlertTargetIds),
        AlertStatusParser.Parse(essentials.MonitorCondition, "Fired", "Resolved", "data.essentials.monitorCondition"),
        essentials.AlertRule ?? string.Empty,
        essentials.AlertRule,
        essentials.Description,
        customProperties?.GetValueOrDefault("service"),
        AlertSeverityMap.FromAzureMonitor(essentials.Severity));
}
