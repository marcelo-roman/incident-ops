using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Application.Incidents;

public record IncidentView
{
    public required Guid Id { get; init; }

    public required int Number { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string ServiceId { get; init; }

    public required Severity Severity { get; init; }

    public required IncidentStatus Status { get; init; }

    public required string? Assignee { get; init; }

    public required int EscalationLevel { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset? AcknowledgedAt { get; init; }

    public required DateTimeOffset? MitigatedAt { get; init; }

    public required DateTimeOffset? ResolvedAt { get; init; }

    public required DateTimeOffset AckDueAt { get; init; }

    public required DateTimeOffset ResolveDueAt { get; init; }

    public required bool AcknowledgementBreached { get; init; }

    public required SlaState SlaState { get; init; }

    public required string? RootCause { get; init; }

    public required IncidentSource Source { get; init; }

    public required string? AlertFingerprint { get; init; }
}
