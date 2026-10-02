using IncidentOps.Application.Incidents.Messaging;

namespace IncidentOps.Application.Tests.Fakes;

public sealed class RecordingNotifier : IIncidentNotifier
{
    public List<IncidentChangeMessage> Messages { get; } = [];

    public Task NotifyAsync(IncidentChangeMessage message, CancellationToken cancellationToken)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}
