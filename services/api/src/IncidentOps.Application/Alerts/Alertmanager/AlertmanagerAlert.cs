namespace IncidentOps.Application.Alerts.Alertmanager;

public sealed record AlertmanagerAlert(
    string? Status,
    IReadOnlyDictionary<string, string>? Labels,
    IReadOnlyDictionary<string, string>? Annotations,
    string? Fingerprint);
