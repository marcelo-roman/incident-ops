using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Watches;
using Microsoft.Extensions.Logging;

namespace IncidentOps.Escalation.Application.UseCases;

public sealed partial class ScheduleAcknowledgementCheck(
    IIncidentEventTranslator translator,
    ISlaCheckScheduler scheduler,
    TimeProvider timeProvider,
    ILogger<ScheduleAcknowledgementCheck> logger)
{
    public async Task<UseCaseResult> ExecuteAsync(InboundMessage message, CancellationToken cancellationToken)
    {
        var translation = translator.Translate(message);
        if (translation is not Translation<AcknowledgementWindowOpened>.Accepted window)
        {
            return UseCaseResult.Skipped(translation.Description);
        }

        var opening = AcknowledgementWatch.Open(window.Value);
        if (opening is not WatchOpening.Opened opened)
        {
            return UseCaseResult.Skipped(opening.Description);
        }

        var check = opened.Watch.Schedule(timeProvider.GetUtcNow());
        await scheduler.ScheduleAsync(check, cancellationToken);
        LogScheduled(check.Key.IncidentId.Value, check.Key.Level.Value, check.CheckAt);
        return UseCaseResult.Done($"acknowledgement check scheduled for {check.CheckAt:O}");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Scheduled acknowledgement check for incident {IncidentId} at level {EscalationLevel} due {CheckAt}")]
    private partial void LogScheduled(Guid incidentId, int escalationLevel, DateTimeOffset checkAt);
}
