using Microsoft.AspNetCore.SignalR;

namespace IncidentOps.Api.RealTime;

public sealed class IncidentsHub : Hub
{
    public const string Path = "/hubs/incidents";
    public const string IncidentChanged = "IncidentChanged";
    public const string TimelineAppended = "TimelineAppended";
}
