using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.Sla;

public sealed record SlaTargets(TimeSpan AcknowledgeWithin, TimeSpan ResolveWithin)
{
    public SlaTargets CompressedBy(double timeScale)
    {
        Guard.Against(double.IsNaN(timeScale) || timeScale <= 0, nameof(timeScale), "The SLA time scale must be greater than zero.");
        return new SlaTargets(AcknowledgeWithin / timeScale, ResolveWithin / timeScale);
    }
}
