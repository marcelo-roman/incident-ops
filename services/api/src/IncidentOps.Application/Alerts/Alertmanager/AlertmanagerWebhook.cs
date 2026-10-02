namespace IncidentOps.Application.Alerts.Alertmanager;

public sealed record AlertmanagerWebhook(
    string? Version,
    string? GroupKey,
    string? Status,
    string? Receiver,
    IReadOnlyList<AlertmanagerAlert>? Alerts);
