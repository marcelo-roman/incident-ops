namespace IncidentOps.Application.Alerts.AzureMonitor;

public sealed record AzureMonitorAlertData(
    AzureMonitorEssentials? Essentials,
    IReadOnlyDictionary<string, string>? CustomProperties);
