using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents.ReadModel;

public sealed class TimelineEntryRecord
{
    public Guid Id { get; init; }

    public Guid IncidentId { get; init; }

    public int Sequence { get; init; }

    public DateTimeOffset At { get; init; }

    public TimelineKind Kind { get; init; }

    public string Actor { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
