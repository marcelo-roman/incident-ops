using System.Text.Json;
using IncidentOps.Application.Common;
using IncidentOps.Application.Common.Outbox;
using IncidentOps.Domain.Common;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Incidents.Events;

namespace IncidentOps.Application.Incidents.Messaging;

public sealed class IncidentEventTranslator : IDomainEventTranslator
{
    public IReadOnlyList<OutboxEntry> Translate(IAggregateRoot aggregate)
    {
        if (aggregate is not Incident incident)
        {
            return [];
        }

        return incident.DomainEvents
            .OfType<IncidentDomainEvent>()
            .Select(domainEvent => ToEntry(incident, domainEvent))
            .ToList();
    }

    private static OutboxEntry ToEntry(Incident incident, IncidentDomainEvent domainEvent)
    {
        var entry = incident.Timeline.Single(item => item.Id == domainEvent.TimelineEntryId);
        var message = new IncidentChangeMessage(
            Guid.NewGuid(),
            IncidentEventTypes.For(domainEvent),
            domainEvent.OccurredAt,
            IncidentViews.From(incident, domainEvent.OccurredAt),
            IncidentViews.From(entry));

        var payload = JsonSerializer.Serialize(message, ContractJson.Options);
        return new OutboxEntry(message.Id, IncidentChangeMessage.MessageType, payload, domainEvent.OccurredAt);
    }
}
