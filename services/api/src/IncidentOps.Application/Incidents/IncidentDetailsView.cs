using System.Diagnostics.CodeAnalysis;

namespace IncidentOps.Application.Incidents;

public sealed record IncidentDetailsView : IncidentView
{
    [SetsRequiredMembers]
    public IncidentDetailsView(IncidentView incident, IReadOnlyList<TimelineEntryView> timeline)
        : base(incident)
    {
        Timeline = timeline;
    }

    public IReadOnlyList<TimelineEntryView> Timeline { get; }
}
