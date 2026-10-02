namespace IncidentOps.Application.Incidents.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(IncidentIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
