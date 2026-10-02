using IncidentOps.Domain.Incidents;

namespace IncidentOps.Domain.Alerts;

public sealed record AlertSignal(
    IncidentSource Source,
    AlertFingerprint Fingerprint,
    AlertStatus Status,
    string? Name,
    string? Summary,
    string? Description,
    string? ServiceLabel,
    Severity Severity);
