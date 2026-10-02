namespace IncidentOps.Escalation.Infrastructure.AntiCorruption;

public sealed record OnCallDto(DateTimeOffset WeekStart, string? Primary, string? Secondary, string? Lead);
