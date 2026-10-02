using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Common;
using IncidentOps.Domain.Errors;
using IncidentOps.Domain.Incidents.Events;
using IncidentOps.Domain.OnCall;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Domain.Incidents;

public sealed class Incident : AggregateRoot<IncidentId>
{
    private readonly List<TimelineEntry> _timeline = [];

    private Incident(IncidentId id, IncidentDraft draft, IncidentSource source, AlertFingerprint? fingerprint, IncidentOpening opening)
        : base(id)
    {
        Number = opening.Number;
        Title = draft.Title;
        Description = draft.Description;
        ServiceId = draft.ServiceId;
        Severity = Guard.Defined(draft.Severity, "severity");
        Status = IncidentStatus.Triggered;
        EscalationLevel = EscalationLevel.First;
        Assignee = opening.Shift.TargetFor(EscalationLevel.First);
        Sla = SlaClock.Start(opening.Targets, opening.At);
        Source = Guard.Defined(source, "source");
        AlertFingerprint = fingerprint;
    }

    private Incident()
    {
    }

    public IncidentNumber Number { get; private set; } = null!;

    public IncidentTitle Title { get; private set; } = null!;

    public Description Description { get; private set; } = null!;

    public ServiceId ServiceId { get; private set; } = null!;

    public Severity Severity { get; private set; }

    public IncidentStatus Status { get; private set; }

    public Actor? Assignee { get; private set; }

    public EscalationLevel EscalationLevel { get; private set; } = null!;

    public SlaClock Sla { get; private set; } = null!;

    public DateTimeOffset CreatedAt => Sla.StartedAt;

    public DateTimeOffset? AcknowledgedAt { get; private set; }

    public DateTimeOffset? MitigatedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public RootCause? RootCause { get; private set; }

    public IncidentSource Source { get; private set; }

    public AlertFingerprint? AlertFingerprint { get; private set; }

    public IReadOnlyList<TimelineEntry> Timeline => _timeline.AsReadOnly();

    public bool IsOpen => Status != IncidentStatus.Resolved;

    public static Incident Trigger(IncidentDraft draft, IncidentOpening opening) =>
        Open(draft, IncidentSource.Manual, null, opening);

    public static Incident TriggerFromAlert(Alert alert, ServiceId serviceId, IncidentOpening opening)
    {
        if (!alert.IsFiring)
        {
            throw new DomainValidationException("status", "Only a firing alert opens an incident.");
        }

        var draft = new IncidentDraft(alert.Title, alert.Description, serviceId, alert.Severity);
        return Open(draft, alert.Source, alert.Fingerprint, opening);
    }

    public void Acknowledge(Actor actor, DateTimeOffset now)
    {
        MoveTo(IncidentStatus.Acknowledged);
        AcknowledgedAt = now;
        Assignee = actor;
        Sla = Sla.RecordAcknowledgement(now);
        var entry = Append(TimelineKind.Acknowledged, actor, $"Acknowledged by {actor}.", now);
        Raise(new IncidentAcknowledged(Id, entry.Id, now));
    }

    public void Mitigate(Actor actor, Note note, DateTimeOffset now)
    {
        MoveTo(IncidentStatus.Mitigated);
        MitigatedAt = now;
        var entry = Append(TimelineKind.Mitigated, actor, note.Value, now);
        Raise(new IncidentMitigated(Id, entry.Id, now));
    }

    public void Resolve(Actor actor, RootCause rootCause, DateTimeOffset now)
    {
        MoveTo(IncidentStatus.Resolved);
        ResolvedAt = now;
        RootCause = rootCause;
        var entry = Append(TimelineKind.Resolved, actor, $"Resolved. Root cause: {rootCause}", now);
        Raise(new IncidentResolved(Id, entry.Id, now));
    }

    public void Escalate(Note reason, OnCallShift shift, SlaTargets targets, DateTimeOffset now)
    {
        if (Status != IncidentStatus.Triggered)
        {
            throw new EscalationNotAllowedException($"Only triggered incidents escalate; this one is {Status}.");
        }

        EscalationLevel = EscalationLevel.Next();
        Assignee = shift.TargetFor(EscalationLevel);
        Sla = Sla.RestartAcknowledgement(targets, now);
        var message = $"Escalated to level {EscalationLevel} ({Assignee}): {reason}";
        var entry = Append(TimelineKind.Escalated, Actor.System, message, now);
        Raise(new IncidentEscalated(Id, entry.Id, now, EscalationLevel));
    }

    public void AddNote(Actor actor, Note note, DateTimeOffset now)
    {
        var entry = Append(TimelineKind.Note, actor, note.Value, now);
        Raise(new IncidentNoteAdded(Id, entry.Id, now));
    }

    public void RecordAlert(Alert alert, DateTimeOffset now)
    {
        if (alert.Fingerprint != AlertFingerprint)
        {
            throw new DomainValidationException("fingerprint", "The alert does not belong to this incident.");
        }

        var entry = Append(TimelineKind.Alert, Actor.Alerting, alert.TimelineMessage, now);
        Raise(new AlertRecorded(Id, entry.Id, now, alert.Fingerprint, alert.Status));
        if (AlertIngestionPolicy.ShouldMitigate(alert.Status, Status))
        {
            Mitigate(Actor.Alerting, new Note("Alert resolved at the source; incident mitigated automatically."), now);
        }
    }

    public SlaState SlaStateAt(DateTimeOffset now) => Sla.StateAt(now, Status, AcknowledgedAt, ResolvedAt);

    public bool CompliesWithSla() => Sla.Complies(AcknowledgedAt, ResolvedAt);

    private static Incident Open(IncidentDraft draft, IncidentSource source, AlertFingerprint? fingerprint, IncidentOpening opening)
    {
        var incident = new Incident(IncidentId.New(), draft, source, fingerprint, opening);
        var message = $"{incident.Severity} triggered from {source}, paged {incident.Assignee}.";
        var entry = incident.Append(TimelineKind.Triggered, Actor.System, message, opening.At);
        incident.Raise(new IncidentTriggered(incident.Id, entry.Id, opening.At));
        return incident;
    }

    private void MoveTo(IncidentStatus next)
    {
        IncidentStateMachine.EnsureCanTransition(Status, next);
        Status = next;
    }

    private TimelineEntry Append(TimelineKind kind, Actor actor, string message, DateTimeOffset at)
    {
        var entry = new TimelineEntry(Id, _timeline.Count + 1, kind, actor, message, at);
        _timeline.Add(entry);
        return entry;
    }
}
