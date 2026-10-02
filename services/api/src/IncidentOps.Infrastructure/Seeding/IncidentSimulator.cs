using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.OnCall;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Infrastructure.Seeding;

internal sealed class IncidentSimulator(SeedRandom random, OnCallRotation rotation)
{
    private const double AcknowledgeBreachChance = 0.08;
    private const double ResolveBreachChance = 0.09;
    private const double FalseAlarmChance = 0.04;
    private const double MitigationChance = 0.7;
    private static readonly RootCause FalseAlarmRootCause = new("Transient alert with no customer impact; alert threshold tuned.");
    private static readonly Note EscalationReason = new("Not acknowledged within SLA.");

    public Incident Simulate(IncidentPlan plan, int number, DateTimeOffset now)
    {
        var targets = SlaPolicy.For(plan.Draft.Severity);
        var opening = new IncidentOpening(new IncidentNumber(number), rotation.ShiftAt(plan.CreatedAt), targets, plan.CreatedAt);
        var incident = Incident.Trigger(plan.Draft, opening);
        var acknowledgeAfter = Delay(targets.AcknowledgeWithin, AcknowledgeBreachChance);

        if (random.Chance(FalseAlarmChance))
        {
            CloseAsFalseAlarm(incident, plan.CreatedAt + (acknowledgeAfter * 0.8), now);
            return incident;
        }

        var acknowledgedAt = plan.CreatedAt + acknowledgeAfter;
        EscalateUntil(incident, Earliest(acknowledgedAt, now), targets);
        if (acknowledgedAt > now)
        {
            return incident;
        }

        incident.Acknowledge(incident.Assignee!, acknowledgedAt);
        var resolveAfter = Delay(targets.ResolveWithin, ResolveBreachChance);
        var resolvedAt = Latest(plan.CreatedAt + resolveAfter, acknowledgedAt + TimeSpan.FromMinutes(5));
        Work(incident, plan.Theme, acknowledgedAt, resolvedAt, now);
        return incident;
    }

    private void Work(Incident incident, IncidentTheme theme, DateTimeOffset acknowledgedAt, DateTimeOffset resolvedAt, DateTimeOffset now)
    {
        var actor = incident.Assignee!;
        var span = resolvedAt - acknowledgedAt;
        var noteTimes = Enumerable.Range(0, random.Between(0, 3))
            .Select(_ => acknowledgedAt + (span * random.Between(0.05, 0.3)))
            .Order()
            .ToList();
        foreach (var at in noteTimes)
        {
            AddNoteIfPast(incident, actor, new Note(random.Pick(theme.Notes)), at, now);
        }

        var mitigatedAt = acknowledgedAt + (span * random.Between(0.35, 0.8));
        if (random.Chance(MitigationChance) && mitigatedAt <= now)
        {
            incident.Mitigate(actor, new Note(random.Pick(theme.Mitigations)), mitigatedAt);
        }

        if (resolvedAt <= now)
        {
            incident.Resolve(actor, new RootCause(random.Pick(theme.RootCauses)), resolvedAt);
        }
    }

    private void EscalateUntil(Incident incident, DateTimeOffset until, SlaTargets targets)
    {
        while (!incident.EscalationLevel.IsLast && incident.Sla.AckDueAt < until)
        {
            var escalatedAt = incident.Sla.AckDueAt;
            incident.Escalate(EscalationReason, rotation.ShiftAt(escalatedAt), targets, escalatedAt);
        }
    }

    private static void CloseAsFalseAlarm(Incident incident, DateTimeOffset resolvedAt, DateTimeOffset now)
    {
        if (resolvedAt > now)
        {
            return;
        }

        incident.Resolve(incident.Assignee!, FalseAlarmRootCause, resolvedAt);
    }

    private static void AddNoteIfPast(Incident incident, Actor actor, Note note, DateTimeOffset at, DateTimeOffset now)
    {
        if (at > now)
        {
            return;
        }

        incident.AddNote(actor, note, at);
    }

    private TimeSpan Delay(TimeSpan window, double breachChance)
    {
        if (random.Chance(breachChance))
        {
            return window * random.Between(1.05, 2.2);
        }

        return window * (0.04 + (0.85 * Math.Pow(random.NextDouble(), 1.6)));
    }

    private static DateTimeOffset Earliest(DateTimeOffset first, DateTimeOffset second) =>
        new(Math.Min(first.UtcTicks, second.UtcTicks), TimeSpan.Zero);

    private static DateTimeOffset Latest(DateTimeOffset first, DateTimeOffset second) =>
        new(Math.Max(first.UtcTicks, second.UtcTicks), TimeSpan.Zero);
}
