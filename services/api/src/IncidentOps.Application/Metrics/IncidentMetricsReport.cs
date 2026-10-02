using IncidentOps.Application.Incidents.ReadModel;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Application.Metrics;

public static class IncidentMetricsReport
{
    public static readonly TimeSpan Window = TimeSpan.FromDays(30);

    public static MetricsSummaryView Summarize(IReadOnlyCollection<IncidentRecord> incidents, DateTimeOffset now)
    {
        var open = incidents.Where(incident => incident.IsOpen).ToList();
        var recent = incidents.Where(incident => incident.CreatedAt >= now - Window).ToList();

        return new MetricsSummaryView(
            CountBySeverity(open),
            CompliancePercent(recent, now),
            MeanMinutes(recent, incident => incident.AcknowledgedAt - incident.CreatedAt),
            MeanMinutes(recent, incident => incident.ResolvedAt - incident.CreatedAt),
            open.Count(incident => incident.SlaStateAt(now) == SlaState.Breached));
    }

    private static Dictionary<Severity, int> CountBySeverity(List<IncidentRecord> open) =>
        Enum.GetValues<Severity>().ToDictionary(
            severity => severity,
            severity => open.Count(incident => incident.Severity == severity));

    private static double CompliancePercent(List<IncidentRecord> recent, DateTimeOffset now)
    {
        var settled = recent.Where(incident => IsSettled(incident, now)).ToList();
        if (settled.Count == 0)
        {
            return 100;
        }

        return Math.Round(100.0 * settled.Count(incident => incident.CompliesWithSla()) / settled.Count, 1);
    }

    private static bool IsSettled(IncidentRecord incident, DateTimeOffset now) =>
        !incident.IsOpen || incident.SlaStateAt(now) == SlaState.Breached;

    private static double MeanMinutes(List<IncidentRecord> incidents, Func<IncidentRecord, TimeSpan?> duration)
    {
        var minutes = incidents
            .Select(duration)
            .OfType<TimeSpan>()
            .Select(span => span.TotalMinutes)
            .ToList();

        if (minutes.Count == 0)
        {
            return 0;
        }

        return Math.Round(minutes.Average(), 1);
    }
}
