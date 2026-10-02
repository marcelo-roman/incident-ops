namespace IncidentOps.Application.Incidents.ReadModel;

public interface IIncidentQueries
{
    Task<IncidentRecord?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TimelineEntryRecord>> TimelineAsync(Guid incidentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<IncidentRecord>> ListAsync(IncidentFilter filter, CancellationToken cancellationToken);

    Task<IReadOnlyList<IncidentRecord>> ListCreatedBetweenAsync(DateTimeOffset createdFrom, DateTimeOffset createdUntil, CancellationToken cancellationToken);

    Task<IReadOnlyList<IncidentRecord>> ListOpenOrCreatedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);
}
