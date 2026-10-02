using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents;
using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Alerts.Ingest;

public sealed class IngestAlertHandler(
    IIncidentRepository incidents,
    IServiceRepository services,
    IncidentOpenings openings,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<AlertOutcome> HandleAsync(Alert alert, CancellationToken cancellationToken)
    {
        var open = await incidents.FindOpenByFingerprintAsync(alert.Fingerprint, cancellationToken);
        return AlertIngestionPolicy.Decide(alert, open) switch
        {
            AlertAction.OpenIncident => await OpenAsync(alert, cancellationToken),
            AlertAction.AppendToIncident => await AppendAsync(open!, alert, cancellationToken),
            _ => new AlertOutcome(alert.Fingerprint.Value, AlertAction.Ignore, null, null),
        };
    }

    private async Task<AlertOutcome> OpenAsync(Alert alert, CancellationToken cancellationToken)
    {
        var serviceId = AlertServiceResolver.Resolve(alert.ServiceLabel, await services.ListIdsAsync(cancellationToken));
        var opening = await openings.NextAsync(alert.Severity, cancellationToken);
        var incident = Incident.TriggerFromAlert(alert, serviceId, opening);

        incidents.Add(incident);
        await unitOfWork.CommitAsync(cancellationToken);
        return Outcome(alert, AlertAction.OpenIncident, incident);
    }

    private async Task<AlertOutcome> AppendAsync(Incident incident, Alert alert, CancellationToken cancellationToken)
    {
        incident.RecordAlert(alert, clock.UtcNow);

        await unitOfWork.CommitAsync(cancellationToken);
        return Outcome(alert, AlertAction.AppendToIncident, incident);
    }

    private static AlertOutcome Outcome(Alert alert, AlertAction action, Incident incident) =>
        new(alert.Fingerprint.Value, action, incident.Id.Value, incident.Number.Value);
}
