namespace IncidentOps.Application.Common.Outbox;

public interface IOutboxMessageHandler
{
    Task HandleAsync(string type, string payload, CancellationToken cancellationToken);
}
