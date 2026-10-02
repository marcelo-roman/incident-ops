using IncidentOps.Application.Common;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents.Notes;

public sealed class AddIncidentNoteHandler(IIncidentRepository incidents, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<TimelineEntryView> HandleAsync(Guid incidentId, AddIncidentNote command, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetRequiredAsync(incidentId, cancellationToken);

        incident.AddNote(new Actor(command.Actor), new Note(command.Message, "message"), clock.UtcNow);

        await unitOfWork.CommitAsync(cancellationToken);
        return IncidentViews.From(incident.Timeline[^1]);
    }
}
