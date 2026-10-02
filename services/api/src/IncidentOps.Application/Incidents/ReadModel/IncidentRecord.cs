using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Application.Incidents.ReadModel;

public sealed class IncidentRecord
{
    public Guid Id { get; init; }

    public int Number { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ServiceId { get; init; } = string.Empty;

    public Severity Severity { get; init; }

    public IncidentStatus Status { get; init; }

    public string? Assignee { get; init; }

    public int EscalationLevel { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? AcknowledgedAt { get; init; }

    public DateTimeOffset? MitigatedAt { get; init; }

    public DateTimeOffset? ResolvedAt { get; init; }

    public DateTimeOffset AckWindowStartsAt { get; init; }

    public DateTimeOffset AckDueAt { get; init; }

    public DateTimeOffset ResolveDueAt { get; init; }

    public bool AcknowledgementBreached { get; init; }

    public string? RootCause { get; init; }

    public IncidentSource Source { get; init; }

    public string? AlertFingerprint { get; init; }

    public bool IsOpen => Status != IncidentStatus.Resolved;

    public SlaClock SlaClock() =>
        Domain.Sla.SlaClock.Restore(CreatedAt, AckWindowStartsAt, AckDueAt, ResolveDueAt, AcknowledgementBreached);

    public SlaState SlaStateAt(DateTimeOffset now) => SlaClock().StateAt(now, Status, AcknowledgedAt, ResolvedAt);

    public bool CompliesWithSla() => SlaClock().Complies(AcknowledgedAt, ResolvedAt);
}
