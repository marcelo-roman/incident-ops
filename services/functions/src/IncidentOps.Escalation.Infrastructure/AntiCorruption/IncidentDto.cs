namespace IncidentOps.Escalation.Infrastructure.AntiCorruption;

public sealed record IncidentDto(
    Guid Id,
    int Number,
    string? Title,
    string? ServiceId,
    string? Severity,
    string? Status,
    int EscalationLevel,
    DateTimeOffset AckDueAt);
