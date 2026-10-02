namespace IncidentOps.Application.Incidents;

public sealed class IncidentNotFoundException(Guid incidentId)
    : Exception($"Incident '{incidentId}' was not found.")
{
    public Guid IncidentId { get; } = incidentId;
}
