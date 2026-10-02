using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Tests.Fakes;

public sealed class InMemoryIncidentStore : IIncidentRepository
{
    private readonly List<Incident> _incidents = [];
    private int _nextNumber = 2001;

    public IReadOnlyList<Incident> All => _incidents;

    public Task<Incident?> FindAsync(IncidentId id, CancellationToken cancellationToken) =>
        Task.FromResult(_incidents.FirstOrDefault(incident => incident.Id == id));

    public Task<Incident?> FindOpenByFingerprintAsync(AlertFingerprint fingerprint, CancellationToken cancellationToken) =>
        Task.FromResult(_incidents.FirstOrDefault(incident => incident.AlertFingerprint == fingerprint && incident.IsOpen));

    public Task<IncidentNumber> NextNumberAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new IncidentNumber(_nextNumber++));

    public void Add(Incident incident) => _incidents.Add(incident);
}
