using IncidentOps.Application.Incidents;
using IncidentOps.Application.Incidents.Acknowledge;
using IncidentOps.Application.Incidents.Listing;
using IncidentOps.Application.Incidents.Mitigate;
using IncidentOps.Application.Incidents.Resolve;
using IncidentOps.Application.Incidents.Trigger;
using IncidentOps.Application.OnCall;
using IncidentOps.Domain.Incidents;
using IncidentOps.Infrastructure.Seeding;
using Microsoft.Extensions.Options;

namespace IncidentOps.Api.Demo;

internal sealed class DemoTrafficScenario(
    ListIncidentsHandler list,
    GetCurrentOnCallHandler onCall,
    TriggerIncidentHandler trigger,
    AcknowledgeIncidentHandler acknowledge,
    MitigateIncidentHandler mitigate,
    ResolveIncidentHandler resolve,
    IOptions<DemoTrafficOptions> options)
{
    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var open = await list.HandleAsync(new ListIncidents(null, null, null, true, 50), cancellationToken);
        if (open.Count < options.Value.TargetOpenIncidents && Random.Shared.NextDouble() < 0.6)
        {
            await TriggerAsync(cancellationToken);
            return;
        }

        if (open.Count == 0)
        {
            return;
        }

        await AdvanceAsync(open[Random.Shared.Next(open.Count)], cancellationToken);
    }

    private Task<IncidentView> TriggerAsync(CancellationToken cancellationToken)
    {
        var theme = IncidentThemeCatalog.All[Random.Shared.Next(IncidentThemeCatalog.All.Count)];
        var severity = (Severity)Random.Shared.Next(1, 4);
        var command = new TriggerIncident(
            theme.Titles[Random.Shared.Next(theme.Titles.Count)],
            theme.Descriptions[Random.Shared.Next(theme.Descriptions.Count)],
            theme.ServiceIds[Random.Shared.Next(theme.ServiceIds.Count)],
            severity);

        return trigger.HandleAsync(command, cancellationToken);
    }

    private async Task AdvanceAsync(IncidentView incident, CancellationToken cancellationToken)
    {
        var shift = await onCall.HandleAsync(cancellationToken);
        var actor = incident.Assignee ?? shift.Primary;
        var step = incident.Status switch
        {
            IncidentStatus.Triggered => acknowledge.HandleAsync(incident.Id, new AcknowledgeIncident(actor), cancellationToken),
            IncidentStatus.Acknowledged => mitigate.HandleAsync(incident.Id, new MitigateIncident(actor, "Mitigation applied by the responder."), cancellationToken),
            _ => resolve.HandleAsync(incident.Id, new ResolveIncident(actor, "Root cause identified and fixed."), cancellationToken),
        };

        await step;
    }
}
