using IncidentOps.Application.Common;
using IncidentOps.Application.Common.Outbox;
using IncidentOps.Domain.Common;
using IncidentOps.Infrastructure.Outbox;
using IncidentOps.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IncidentOps.Infrastructure.Persistence;

internal sealed class UnitOfWork(
    IncidentOpsDbContext db,
    IEnumerable<IDomainEventTranslator> translators,
    OutboxSignal signal) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        var aggregates = db.ChangeTracker.Entries<IAggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        db.OutboxMessages.AddRange(aggregates
            .SelectMany(aggregate => translators.SelectMany(translator => translator.Translate(aggregate)))
            .Select(OutboxMessage.From));

        await SaveAsync(cancellationToken);
        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());
        signal.Notify();
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(exception);
        }
        catch (DbUpdateException exception) when (UniqueConstraint.IsViolatedBy(exception))
        {
            throw new ConcurrencyConflictException(exception);
        }
    }
}
