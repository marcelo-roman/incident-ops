using System.Collections.Concurrent;
using IncidentOps.Application.Incidents.Messaging;

namespace IncidentOps.Api.Tests.Infrastructure;

public sealed class RecordingEventPublisher : IEventPublisher
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly ConcurrentQueue<IncidentIntegrationEvent> _events = new();

    public int FailuresLeft { get; set; }

    public Task PublishAsync(IncidentIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (FailuresLeft > 0)
        {
            FailuresLeft--;
            throw new InvalidOperationException("Broker unavailable.");
        }

        _events.Enqueue(integrationEvent);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<string>> WaitForTypesAsync(Guid incidentId, int count)
    {
        var deadline = DateTimeOffset.UtcNow + Timeout;
        while (TypesFor(incidentId).Count < count && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        await Task.Delay(300);
        return TypesFor(incidentId);
    }

    private List<string> TypesFor(Guid incidentId) =>
        _events.Where(e => e.Incident.Id == incidentId).Select(e => e.Type).ToList();
}
