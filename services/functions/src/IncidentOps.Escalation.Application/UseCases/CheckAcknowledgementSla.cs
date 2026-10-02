using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Watches;
using Microsoft.Extensions.Logging;

namespace IncidentOps.Escalation.Application.UseCases;

public sealed partial class CheckAcknowledgementSla(
    IAcknowledgementCheckTranslator translator,
    IIncidentReader reader,
    IIncidentEscalator escalator,
    TimeProvider timeProvider,
    ILogger<CheckAcknowledgementSla> logger)
{
    public async Task<UseCaseResult> ExecuteAsync(InboundMessage message, CancellationToken cancellationToken)
    {
        var watch = translator.Translate(message);
        var current = await reader.FindAsync(watch.IncidentId, cancellationToken);
        if (current is null)
        {
            return UseCaseResult.Skipped("incident not found");
        }

        var decision = watch.Evaluate(current, timeProvider.GetUtcNow());
        if (decision is not EscalationDecision.Escalate escalate)
        {
            return UseCaseResult.Skipped(decision.Description);
        }

        var attempt = await escalator.EscalateAsync(watch.IncidentId, escalate.Reason, cancellationToken);
        LogEscalationAttempt(watch.IncidentId.Value, watch.Level.Value, escalate.Overdue, attempt);
        return Describe(attempt);
    }

    private static UseCaseResult Describe(EscalationAttempt attempt) => attempt switch
    {
        EscalationAttempt.Escalated => UseCaseResult.Done("incident escalated"),
        EscalationAttempt.Rejected => UseCaseResult.Skipped("escalation rejected because the incident changed meanwhile"),
        _ => UseCaseResult.Skipped("incident not found when escalating"),
    };

    [LoggerMessage(Level = LogLevel.Information, Message = "Acknowledgement SLA breached for incident {IncidentId} at level {EscalationLevel}, overdue by {Overdue}; escalation {EscalationAttempt}")]
    private partial void LogEscalationAttempt(Guid incidentId, int escalationLevel, TimeSpan overdue, EscalationAttempt escalationAttempt);
}
