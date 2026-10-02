using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Metrics;

public sealed record MetricsSummaryView(
    IReadOnlyDictionary<Severity, int> OpenBySeverity,
    double SlaCompliance30d,
    double Mtta30dMinutes,
    double Mttr30dMinutes,
    int BreachedOpen);
