using IncidentOps.Domain.OnCall;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Domain.Incidents;

public sealed record IncidentOpening(IncidentNumber Number, OnCallShift Shift, SlaTargets Targets, DateTimeOffset At);
