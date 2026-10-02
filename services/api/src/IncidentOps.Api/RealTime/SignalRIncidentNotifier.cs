using IncidentOps.Application.Incidents.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace IncidentOps.Api.RealTime;

internal sealed class SignalRIncidentNotifier(IHubContext<IncidentsHub> hub) : IIncidentNotifier
{
    public async Task NotifyAsync(IncidentChangeMessage message, CancellationToken cancellationToken)
    {
        await hub.Clients.All.SendAsync(IncidentsHub.IncidentChanged, message.Incident, cancellationToken);
        await hub.Clients.All.SendAsync(IncidentsHub.TimelineAppended, message.Entry, cancellationToken);
    }
}
