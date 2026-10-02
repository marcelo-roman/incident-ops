using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents.Trigger;

public sealed record TriggerIncident(string Title, string Description, string ServiceId, Severity Severity);
