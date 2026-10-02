namespace IncidentOps.Domain.OnCall;

public interface IOnCallRotationRepository
{
    Task<OnCallRotation> GetAsync(CancellationToken cancellationToken);
}
