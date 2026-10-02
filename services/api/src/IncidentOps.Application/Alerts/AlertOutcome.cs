using IncidentOps.Domain.Alerts;

namespace IncidentOps.Application.Alerts;

public sealed record AlertOutcome(string Fingerprint, AlertAction Action, Guid? IncidentId, int? IncidentNumber);
