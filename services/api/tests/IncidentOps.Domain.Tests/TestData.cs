using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.OnCall;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Domain.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 2, 14, 0, 0, TimeSpan.Zero);

    public static readonly Actor Ava = new("Ava");

    public static readonly OnCallShift Shift = new(Now.AddDays(-2), new Actor("Primary"), new Actor("Secondary"), new Actor("Lead"));

    public static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById(OnCallRotation.TimeZoneId);

    public static readonly ServiceId Checkout = new("checkout");

    public static IncidentOpening Opening(Severity severity = Severity.Sev2, DateTimeOffset? at = null, int number = 1001) =>
        new(new IncidentNumber(number), Shift, SlaPolicy.For(severity), at ?? Now);

    public static IncidentDraft Draft(Severity severity = Severity.Sev2) =>
        new(new IncidentTitle("Checkout latency above 5s"), new Description("p95 above objective"), Checkout, severity);

    public static Incident Incident(Severity severity = Severity.Sev2, DateTimeOffset? at = null) =>
        Incidents.Incident.Trigger(Draft(severity), Opening(severity, at));

    public static Alert Alert(AlertStatus status = AlertStatus.Firing, string fingerprint = "fp-1", string? summary = "API error rate above 5%") =>
        Alerts.Alert.From(new AlertSignal(
            IncidentSource.Alertmanager,
            new AlertFingerprint(fingerprint),
            status,
            "ApiHighErrorRate",
            summary,
            "5xx ratio above threshold for 5 minutes",
            "checkout",
            Severity.Sev2));

    public static Incident AlertIncident(DateTimeOffset? at = null) =>
        Incidents.Incident.TriggerFromAlert(Alert(), Checkout, Opening(Severity.Sev2, at));

    public static Note Note(string text = "Working on it") => new(text);

    public static RootCause RootCause(string text = "Bad deploy") => new(text);
}
