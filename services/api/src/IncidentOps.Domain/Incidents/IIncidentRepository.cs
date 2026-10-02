using IncidentOps.Domain.Alerts;

namespace IncidentOps.Domain.Incidents;

public interface IIncidentRepository
{
    Task<Incident?> FindAsync(IncidentId id, CancellationToken cancellationToken);

    Task<Incident?> FindOpenByFingerprintAsync(AlertFingerprint fingerprint, CancellationToken cancellationToken);

    Task<IncidentNumber> NextNumberAsync(CancellationToken cancellationToken);

    void Add(Incident incident);
}
