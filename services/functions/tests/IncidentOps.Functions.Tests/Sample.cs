using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Incidents;
using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Functions.Tests;

internal static class Sample
{
    public static readonly DateTimeOffset Now = new(2026, 10, 2, 14, 0, 0, TimeSpan.Zero);
    public static readonly Guid IncidentGuid = Guid.Parse("6f1c2a52-8f0e-4bfa-9d3c-3f7b3f3a1b11");
    public static readonly IncidentId IncidentId = IncidentId.From(IncidentGuid);
    public static readonly AcknowledgementDeadline Deadline = AcknowledgementDeadline.At(Now.AddMinutes(15));

    public static IncidentProfile Profile(Severity? severity = null) => new(
        IncidentId,
        IncidentNumber.From(1042),
        "Checkout returns 502",
        severity ?? Severity.Sev1,
        ServiceId.From("checkout"));

    public static AcknowledgementWindowOpened Window(
        int level = 1,
        AcknowledgementState acknowledgement = AcknowledgementState.Pending,
        Severity? severity = null) =>
        new(Profile(severity), EscalationLevel.From(level), Deadline, acknowledgement);

    public static AcknowledgementWatch Watch(int level = 1) =>
        AcknowledgementWatch.Resume(IncidentId, EscalationLevel.From(level), Deadline);

    public static IncidentState State(int level = 1, AcknowledgementState acknowledgement = AcknowledgementState.Pending) =>
        new(IncidentId, EscalationLevel.From(level), acknowledgement);

    public static OnCallRotation Rotation() => new(
        OnCallTarget.Named("ana.silva"),
        OnCallTarget.Named("bruno.costa"),
        OnCallTarget.Named("carla.mendes"));
}
