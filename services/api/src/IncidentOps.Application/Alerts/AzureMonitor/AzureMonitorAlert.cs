namespace IncidentOps.Application.Alerts.AzureMonitor;

public sealed record AzureMonitorAlert(string? SchemaId, AzureMonitorAlertData? Data);
