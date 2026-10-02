using IncidentOps.Domain.Common;

namespace IncidentOps.Application.Common.Outbox;

public interface IDomainEventTranslator
{
    IReadOnlyList<OutboxEntry> Translate(IAggregateRoot aggregate);
}
