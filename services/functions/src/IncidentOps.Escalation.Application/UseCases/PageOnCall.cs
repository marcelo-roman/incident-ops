using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Paging;
using IncidentOps.Escalation.Domain.Watches;
using Microsoft.Extensions.Logging;

namespace IncidentOps.Escalation.Application.UseCases;

public sealed partial class PageOnCall(
    IIncidentEventTranslator translator,
    IOnCallDirectory directory,
    IPager pager,
    ILogger<PageOnCall> logger)
{
    public async Task<UseCaseResult> ExecuteAsync(InboundMessage message, CancellationToken cancellationToken)
    {
        var translation = translator.Translate(message);
        if (translation is not Translation<AcknowledgementWindowOpened>.Accepted window)
        {
            return UseCaseResult.Skipped(translation.Description);
        }

        var rotation = await directory.GetCurrentAsync(cancellationToken);
        var decision = PagingDecision.Decide(window.Value, rotation);
        if (decision is not PagingDecision.Page page)
        {
            return UseCaseResult.Skipped(decision.Description);
        }

        await pager.PageAsync(page.Request, cancellationToken);
        LogPaged(page.Request.Incident.Id.Value, page.Request.Level.Value, page.Request.Target.Name);
        return UseCaseResult.Done(decision.Description);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Paged {Target} about incident {IncidentId} at level {EscalationLevel}")]
    private partial void LogPaged(Guid incidentId, int escalationLevel, string target);
}
