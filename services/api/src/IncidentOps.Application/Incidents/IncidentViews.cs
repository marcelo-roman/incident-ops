using IncidentOps.Application.Incidents.ReadModel;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Incidents;

public static class IncidentViews
{
    public static IncidentView From(Incident incident, DateTimeOffset now) => new()
    {
        Id = incident.Id.Value,
        Number = incident.Number.Value,
        Title = incident.Title.Value,
        Description = incident.Description.Value,
        ServiceId = incident.ServiceId.Value,
        Severity = incident.Severity,
        Status = incident.Status,
        Assignee = incident.Assignee?.Value,
        EscalationLevel = incident.EscalationLevel.Value,
        CreatedAt = incident.CreatedAt,
        AcknowledgedAt = incident.AcknowledgedAt,
        MitigatedAt = incident.MitigatedAt,
        ResolvedAt = incident.ResolvedAt,
        AckDueAt = incident.Sla.AckDueAt,
        ResolveDueAt = incident.Sla.ResolveDueAt,
        AcknowledgementBreached = incident.Sla.AcknowledgementBreached,
        SlaState = incident.SlaStateAt(now),
        RootCause = incident.RootCause?.Value,
        Source = incident.Source,
        AlertFingerprint = incident.AlertFingerprint?.Value,
    };

    public static IncidentView From(IncidentRecord record, DateTimeOffset now) => new()
    {
        Id = record.Id,
        Number = record.Number,
        Title = record.Title,
        Description = record.Description,
        ServiceId = record.ServiceId,
        Severity = record.Severity,
        Status = record.Status,
        Assignee = record.Assignee,
        EscalationLevel = record.EscalationLevel,
        CreatedAt = record.CreatedAt,
        AcknowledgedAt = record.AcknowledgedAt,
        MitigatedAt = record.MitigatedAt,
        ResolvedAt = record.ResolvedAt,
        AckDueAt = record.AckDueAt,
        ResolveDueAt = record.ResolveDueAt,
        AcknowledgementBreached = record.AcknowledgementBreached,
        SlaState = record.SlaStateAt(now),
        RootCause = record.RootCause,
        Source = record.Source,
        AlertFingerprint = record.AlertFingerprint,
    };

    public static IReadOnlyList<IncidentView> From(IEnumerable<IncidentRecord> records, DateTimeOffset now) =>
        records.Select(record => From(record, now)).ToList();

    public static TimelineEntryView From(TimelineEntry entry) =>
        new(entry.Id, entry.IncidentId.Value, entry.At, entry.Kind, entry.Actor.Value, entry.Message);

    public static TimelineEntryView From(TimelineEntryRecord entry) =>
        new(entry.Id, entry.IncidentId, entry.At, entry.Kind, entry.Actor, entry.Message);
}
