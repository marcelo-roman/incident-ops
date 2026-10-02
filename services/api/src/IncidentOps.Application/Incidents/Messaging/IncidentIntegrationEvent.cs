namespace IncidentOps.Application.Incidents.Messaging;

public sealed record IncidentIntegrationEvent(Guid Id, string Type, DateTimeOffset Time, IncidentView Incident);
