using IncidentOps.Domain.Alerts;

namespace IncidentOps.Application.Alerts.Ingest;

public sealed class AlertBatchIngestion(IngestAlertHandler ingest)
{
    public async Task<AlertIngestionResult> IngestAsync(IReadOnlyList<AlertSignal> signals, CancellationToken cancellationToken)
    {
        var alerts = signals.Select(Alert.From).ToList();
        var outcomes = new List<AlertOutcome>(alerts.Count);
        foreach (var alert in alerts)
        {
            outcomes.Add(await ingest.HandleAsync(alert, cancellationToken));
        }

        return new AlertIngestionResult(outcomes);
    }
}
