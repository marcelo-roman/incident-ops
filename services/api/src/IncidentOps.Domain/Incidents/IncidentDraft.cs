using IncidentOps.Domain.Catalog;

namespace IncidentOps.Domain.Incidents;

public sealed record IncidentDraft(IncidentTitle Title, Description Description, ServiceId ServiceId, Severity Severity);
