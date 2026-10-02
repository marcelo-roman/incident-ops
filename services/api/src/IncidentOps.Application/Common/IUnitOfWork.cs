namespace IncidentOps.Application.Common;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken);
}
