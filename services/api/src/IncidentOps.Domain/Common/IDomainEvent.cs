namespace IncidentOps.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
