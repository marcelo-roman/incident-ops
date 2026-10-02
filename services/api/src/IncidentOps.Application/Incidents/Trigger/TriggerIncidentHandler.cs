using IncidentOps.Application.Common;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents.Trigger;

public sealed class TriggerIncidentHandler(
    IServiceRepository services,
    IIncidentRepository incidents,
    IncidentOpenings openings,
    IUnitOfWork unitOfWork)
{
    public async Task<IncidentView> HandleAsync(TriggerIncident command, CancellationToken cancellationToken)
    {
        var serviceId = await ExistingServiceAsync(command.ServiceId, cancellationToken);
        var draft = new IncidentDraft(new IncidentTitle(command.Title), new Description(command.Description), serviceId, command.Severity);
        var opening = await openings.NextAsync(command.Severity, cancellationToken);
        var incident = Incident.Trigger(draft, opening);

        incidents.Add(incident);
        await unitOfWork.CommitAsync(cancellationToken);
        return IncidentViews.From(incident, opening.At);
    }

    private async Task<ServiceId> ExistingServiceAsync(string serviceId, CancellationToken cancellationToken)
    {
        var id = new ServiceId(serviceId);
        if (await services.ExistsAsync(id, cancellationToken))
        {
            return id;
        }

        throw new RequestValidationException("serviceId", $"Service '{serviceId}' does not exist.");
    }
}
