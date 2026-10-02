using IncidentOps.Application.Incidents.Messaging;

namespace IncidentOps.Application.Tests.Fakes;

public sealed class RecordingEventPublisher : IEventPublisher
{
    public List<IncidentIntegrationEvent> Events { get; } = [];

    public int FailuresLeft { get; set; }

    public IEnumerable<string> Types => Events.Select(integrationEvent => integrationEvent.Type);

    public Task PublishAsync(IncidentIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (FailuresLeft > 0)
        {
            FailuresLeft--;
            throw new InvalidOperationException("Broker unavailable.");
        }

        Events.Add(integrationEvent);
        return Task.CompletedTask;
    }
}
