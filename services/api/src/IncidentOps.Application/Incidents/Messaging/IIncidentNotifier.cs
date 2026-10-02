namespace IncidentOps.Application.Incidents.Messaging;

public interface IIncidentNotifier
{
    Task NotifyAsync(IncidentChangeMessage message, CancellationToken cancellationToken);
}
