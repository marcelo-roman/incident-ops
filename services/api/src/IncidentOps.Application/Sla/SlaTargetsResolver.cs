using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Sla;
using Microsoft.Extensions.Options;

namespace IncidentOps.Application.Sla;

public sealed class SlaTargetsResolver(IOptions<SlaOptions> options)
{
    public SlaTargets For(Severity severity) => SlaPolicy.For(severity).CompressedBy(options.Value.TimeScale);
}
