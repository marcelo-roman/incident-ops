using IncidentOps.Application.Alerts.Ingest;

namespace IncidentOps.Application.Alerts.AzureMonitor;

public sealed class IngestAzureMonitorAlertHandler(AlertBatchIngestion ingestion)
{
    public Task<AlertIngestionResult> HandleAsync(AzureMonitorAlert alert, CancellationToken cancellationToken) =>
        ingestion.IngestAsync(AzureMonitorTranslator.ToSignals(alert), cancellationToken);
}
