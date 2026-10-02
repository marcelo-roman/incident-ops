using IncidentOps.Application.Common;
using IncidentOps.Application.Common.Outbox;

namespace IncidentOps.Application.Tests.Fakes;

public sealed class CapturingUnitOfWork(InMemoryIncidentStore store, IDomainEventTranslator translator) : IUnitOfWork
{
    public List<OutboxEntry> Outbox { get; } = [];

    public int Commits { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        foreach (var incident in store.All)
        {
            Outbox.AddRange(translator.Translate(incident));
            incident.ClearDomainEvents();
        }

        Commits++;
        return Task.CompletedTask;
    }
}
