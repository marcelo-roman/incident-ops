using System.Diagnostics.Metrics;
using IncidentOps.Application.Incidents;
using IncidentOps.Application.Metrics;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Api.Observability;

public sealed class IncidentOpsMetrics : IDisposable
{
    public const string MeterName = "IncidentOps.Api";

    private readonly Meter _meter;
    private readonly Counter<long> _incidentEvents;
    private volatile MetricsSummaryView? _summary;

    public IncidentOpsMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        _incidentEvents = _meter.CreateCounter<long>(
            "incidentops.incident.events",
            description: "Incident timeline events by kind, severity and source.");
        _meter.CreateObservableGauge(
            "incidentops.incidents.open",
            ObserveOpenIncidents,
            description: "Open incidents by severity.");
        _meter.CreateObservableGauge(
            "incidentops.sla.breached.open",
            () => _summary?.BreachedOpen ?? 0,
            description: "Open incidents whose SLA is breached.");
        _meter.CreateObservableGauge(
            "incidentops.sla.compliance.30d",
            () => _summary?.SlaCompliance30d ?? 100,
            description: "SLA compliance over the last 30 days, in percent.");
    }

    public void RecordEvent(TimelineKind kind, IncidentView incident) =>
        _incidentEvents.Add(
            1,
            new KeyValuePair<string, object?>("kind", kind.ToString()),
            new KeyValuePair<string, object?>("severity", incident.Severity.ToString()),
            new KeyValuePair<string, object?>("source", incident.Source.ToString()));

    public void UpdateSummary(MetricsSummaryView summary) => _summary = summary;

    public void Dispose() => _meter.Dispose();

    private IEnumerable<Measurement<int>> ObserveOpenIncidents()
    {
        var summary = _summary;
        if (summary is null)
        {
            return [];
        }

        return summary.OpenBySeverity.Select(pair =>
            new Measurement<int>(pair.Value, new KeyValuePair<string, object?>("severity", pair.Key.ToString())));
    }
}
