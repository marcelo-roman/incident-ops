namespace IncidentOps.Escalation.Infrastructure.Paging;

public sealed record PagePayload(
    int IncidentNumber,
    string Title,
    string Severity,
    string ServiceId,
    int EscalationLevel,
    string Target,
    Uri Url);
