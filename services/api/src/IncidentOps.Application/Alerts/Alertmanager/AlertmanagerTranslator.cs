using IncidentOps.Application.Common;
using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Alerts.Alertmanager;

public static class AlertmanagerTranslator
{
    public const string SupportedVersion = "4";
    private const string DefaultName = "alertmanager-alert";

    public static IReadOnlyList<AlertSignal> ToSignals(AlertmanagerWebhook webhook)
    {
        if (webhook.Version != SupportedVersion)
        {
            throw new RequestValidationException("version", $"Only Alertmanager webhook version {SupportedVersion} is supported.");
        }

        return (webhook.Alerts ?? []).Select(ToSignal).ToList();
    }

    private static AlertSignal ToSignal(AlertmanagerAlert alert) => new(
        IncidentSource.Alertmanager,
        AlertFingerprint.FromAlertmanager(alert.Fingerprint),
        AlertStatusParser.Parse(alert.Status, "firing", "resolved", "alerts.status"),
        ValueOf(alert.Labels, "alertname") ?? DefaultName,
        ValueOf(alert.Annotations, "summary"),
        ValueOf(alert.Annotations, "description"),
        ValueOf(alert.Labels, "service"),
        AlertSeverityMap.FromAlertmanager(ValueOf(alert.Labels, "severity")));

    private static string? ValueOf(IReadOnlyDictionary<string, string>? values, string key)
    {
        if (values is null)
        {
            return null;
        }

        return values.GetValueOrDefault(key);
    }
}
