using System.Globalization;
using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;

namespace IncidentOps.Escalation.Domain.Watches;

public sealed record WatchKey
{
    public WatchKey(IncidentId incidentId, EscalationLevel level)
    {
        ArgumentNullException.ThrowIfNull(incidentId);
        ArgumentNullException.ThrowIfNull(level);
        IncidentId = incidentId;
        Level = level;
    }

    public IncidentId IncidentId { get; }

    public EscalationLevel Level { get; }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{IncidentId.Value}-{Level.Value}");
}
