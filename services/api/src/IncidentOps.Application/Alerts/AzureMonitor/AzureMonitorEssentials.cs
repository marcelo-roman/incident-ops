namespace IncidentOps.Application.Alerts.AzureMonitor;

public sealed record AzureMonitorEssentials(
    string? AlertId,
    string? AlertRule,
    string? Severity,
    string? MonitorCondition,
    string? Description,
    IReadOnlyList<string>? AlertTargetIds);
