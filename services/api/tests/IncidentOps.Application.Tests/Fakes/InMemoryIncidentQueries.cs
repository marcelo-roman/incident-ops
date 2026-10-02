using IncidentOps.Application.Incidents.ReadModel;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Tests.Fakes;

public sealed class InMemoryIncidentQueries(InMemoryIncidentStore store) : IIncidentQueries
{
    public List<IncidentRecord> Extra { get; } = [];

    public Task<IncidentRecord?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Records().FirstOrDefault(record => record.Id == id));

    public Task<IReadOnlyList<TimelineEntryRecord>> TimelineAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        IReadOnlyList<TimelineEntryRecord> entries = store.All
            .Where(incident => incident.Id.Value == incidentId)
            .SelectMany(incident => incident.Timeline)
            .Select(entry => new TimelineEntryRecord
            {
                Id = entry.Id,
                IncidentId = entry.IncidentId.Value,
                Sequence = entry.Sequence,
                At = entry.At,
                Kind = entry.Kind,
                Actor = entry.Actor.Value,
                Message = entry.Message,
            })
            .ToList();

        return Task.FromResult(entries);
    }

    public Task<IReadOnlyList<IncidentRecord>> ListAsync(IncidentFilter filter, CancellationToken cancellationToken)
    {
        IReadOnlyList<IncidentRecord> matches = Records()
            .Where(record => filter.Status is null || record.Status == filter.Status)
            .Where(record => filter.Severity is null || record.Severity == filter.Severity)
            .Where(record => filter.ServiceId is null || record.ServiceId == filter.ServiceId)
            .Where(record => filter.Open is null || record.IsOpen == filter.Open)
            .OrderByDescending(record => record.CreatedAt)
            .Take(filter.Limit)
            .ToList();

        return Task.FromResult(matches);
    }

    public Task<IReadOnlyList<IncidentRecord>> ListCreatedBetweenAsync(DateTimeOffset createdFrom, DateTimeOffset createdUntil, CancellationToken cancellationToken)
    {
        IReadOnlyList<IncidentRecord> matches = Records()
            .Where(record => record.CreatedAt >= createdFrom && record.CreatedAt <= createdUntil)
            .ToList();

        return Task.FromResult(matches);
    }

    public Task<IReadOnlyList<IncidentRecord>> ListOpenOrCreatedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        IReadOnlyList<IncidentRecord> matches = Records()
            .Where(record => record.IsOpen || record.CreatedAt >= since)
            .ToList();

        return Task.FromResult(matches);
    }

    public static IncidentRecord Project(Incident incident) => new()
    {
        Id = incident.Id.Value,
        Number = incident.Number.Value,
        Title = incident.Title.Value,
        Description = incident.Description.Value,
        ServiceId = incident.ServiceId.Value,
        Severity = incident.Severity,
        Status = incident.Status,
        Assignee = incident.Assignee?.Value,
        EscalationLevel = incident.EscalationLevel.Value,
        CreatedAt = incident.CreatedAt,
        AcknowledgedAt = incident.AcknowledgedAt,
        MitigatedAt = incident.MitigatedAt,
        ResolvedAt = incident.ResolvedAt,
        AckWindowStartsAt = incident.Sla.AckWindowStartsAt,
        AckDueAt = incident.Sla.AckDueAt,
        ResolveDueAt = incident.Sla.ResolveDueAt,
        AcknowledgementBreached = incident.Sla.AcknowledgementBreached,
        RootCause = incident.RootCause?.Value,
        Source = incident.Source,
        AlertFingerprint = incident.AlertFingerprint?.Value,
    };

    private IEnumerable<IncidentRecord> Records() => store.All.Select(Project).Concat(Extra);
}
