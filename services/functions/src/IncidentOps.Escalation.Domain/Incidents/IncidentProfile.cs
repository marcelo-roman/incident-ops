namespace IncidentOps.Escalation.Domain.Incidents;

public sealed record IncidentProfile
{
    public IncidentProfile(IncidentId id, IncidentNumber number, string title, Severity severity, ServiceId serviceId)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(severity);
        ArgumentNullException.ThrowIfNull(serviceId);
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException($"Incident {id} has no title.");
        }

        Id = id;
        Number = number;
        Title = title;
        Severity = severity;
        ServiceId = serviceId;
    }

    public IncidentId Id { get; }

    public IncidentNumber Number { get; }

    public string Title { get; }

    public Severity Severity { get; }

    public ServiceId ServiceId { get; }
}
