using IncidentOps.Application.Incidents.Messaging;

namespace IncidentOps.Api.Observability;

internal sealed class MetricsIncidentNotifier(IncidentOpsMetrics metrics) : IIncidentNotifier
{
    public Task NotifyAsync(IncidentChangeMessage message, CancellationToken cancellationToken)
    {
        metrics.RecordEvent(message.Entry.Kind, message.Incident);
        return Task.CompletedTask;
    }
}
