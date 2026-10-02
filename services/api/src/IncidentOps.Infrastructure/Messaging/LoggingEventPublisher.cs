using IncidentOps.Application.Incidents.Messaging;
using Microsoft.Extensions.Logging;

namespace IncidentOps.Infrastructure.Messaging;

internal sealed partial class LoggingEventPublisher(ILogger<LoggingEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync(IncidentIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        LogEvent(integrationEvent.Type, integrationEvent.Incident.Number, integrationEvent.Incident.Severity.ToString());
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Service Bus is not configured; {EventType} for INC-{IncidentNumber} ({Severity}) was only logged")]
    private partial void LogEvent(string eventType, int incidentNumber, string severity);
}
