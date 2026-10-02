using IncidentOps.Escalation.Domain.Paging;

namespace IncidentOps.Escalation.Infrastructure.Paging;

public sealed class PagePayloadTranslator(IncidentLinks links)
{
    public PagePayload ToPayload(PageRequest request) => new(
        request.Incident.Number.Value,
        request.Incident.Title,
        request.Incident.Severity.Name,
        request.Incident.ServiceId.Value,
        request.Level.Value,
        request.Target.Name,
        links.For(request.Incident.Id));
}
