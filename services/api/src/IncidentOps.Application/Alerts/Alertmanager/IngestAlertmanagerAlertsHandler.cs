using IncidentOps.Application.Alerts.Ingest;

namespace IncidentOps.Application.Alerts.Alertmanager;

public sealed class IngestAlertmanagerAlertsHandler(AlertBatchIngestion ingestion)
{
    public Task<AlertIngestionResult> HandleAsync(AlertmanagerWebhook webhook, CancellationToken cancellationToken) =>
        ingestion.IngestAsync(AlertmanagerTranslator.ToSignals(webhook), cancellationToken);
}
