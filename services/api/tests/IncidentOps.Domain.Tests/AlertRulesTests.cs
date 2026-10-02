using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Errors;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Incidents.Events;

namespace IncidentOps.Domain.Tests;

public class AlertRulesTests
{
    private static readonly ServiceId[] KnownServices = [new("checkout"), new("identity"), ServiceId.Platform];

    [Theory]
    [InlineData("critical", Severity.Sev1)]
    [InlineData("CRITICAL", Severity.Sev1)]
    [InlineData("high", Severity.Sev2)]
    [InlineData("error", Severity.Sev2)]
    [InlineData("warning", Severity.Sev3)]
    [InlineData("info", Severity.Sev4)]
    [InlineData(null, Severity.Sev4)]
    [InlineData("unexpected", Severity.Sev4)]
    public void Alertmanager_severity_label_maps_to_incident_severity(string? label, Severity expected)
    {
        AlertSeverityMap.FromAlertmanager(label).Should().Be(expected);
    }

    [Theory]
    [InlineData("Sev0", Severity.Sev1)]
    [InlineData("Sev1", Severity.Sev1)]
    [InlineData("Sev2", Severity.Sev2)]
    [InlineData("Sev3", Severity.Sev3)]
    [InlineData("Sev4", Severity.Sev4)]
    [InlineData(null, Severity.Sev4)]
    public void Azure_monitor_severity_maps_to_incident_severity(string? severity, Severity expected)
    {
        AlertSeverityMap.FromAzureMonitor(severity).Should().Be(expected);
    }

    [Fact]
    public void Fingerprints_follow_each_source_rule()
    {
        AlertFingerprint.FromAlertmanager(" 4f1c2d ").Value.Should().Be("4f1c2d");
        AlertFingerprint.FromAzureMonitor("api-failed", ["/subscriptions/1/app-a", "/subscriptions/1/app-b"]).Value
            .Should().Be("api-failed|/subscriptions/1/app-a");
        AlertFingerprint.FromAzureMonitor("api-availability", []).Value.Should().Be("api-availability|");
    }

    [Fact]
    public void Missing_fingerprint_or_rule_is_rejected()
    {
        var alertmanager = () => AlertFingerprint.FromAlertmanager(null);
        var azure = () => AlertFingerprint.FromAzureMonitor(" ", null);

        alertmanager.Should().Throw<DomainValidationException>();
        azure.Should().Throw<DomainValidationException>().Which.Field.Should().Be("alertRule");
    }

    [Theory]
    [InlineData("checkout", "checkout")]
    [InlineData("Identity", "identity")]
    [InlineData("unknown-service", "platform")]
    [InlineData("", "platform")]
    [InlineData(null, "platform")]
    public void Service_label_falls_back_to_platform(string? label, string expected)
    {
        AlertServiceResolver.Resolve(label, KnownServices).Value.Should().Be(expected);
    }

    [Fact]
    public void Ingestion_policy_dedupes_by_open_incident()
    {
        var open = TestData.AlertIncident();

        AlertIngestionPolicy.Decide(TestData.Alert(), null).Should().Be(AlertAction.OpenIncident);
        AlertIngestionPolicy.Decide(TestData.Alert(), open).Should().Be(AlertAction.AppendToIncident);
        AlertIngestionPolicy.Decide(TestData.Alert(AlertStatus.Resolved), open).Should().Be(AlertAction.AppendToIncident);
        AlertIngestionPolicy.Decide(TestData.Alert(AlertStatus.Resolved), null).Should().Be(AlertAction.Ignore);
    }

    [Fact]
    public void Firing_alert_opens_an_incident_with_source_and_fingerprint()
    {
        var incident = TestData.AlertIncident();

        incident.Source.Should().Be(IncidentSource.Alertmanager);
        incident.AlertFingerprint.Should().Be(new AlertFingerprint("fp-1"));
        incident.Title.Value.Should().Be("API error rate above 5%");
        incident.Description.Value.Should().Be("5xx ratio above threshold for 5 minutes");
        incident.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<IncidentTriggered>();
    }

    [Fact]
    public void Resolved_alert_cannot_open_an_incident()
    {
        var act = () => Incident.TriggerFromAlert(TestData.Alert(AlertStatus.Resolved), TestData.Checkout, TestData.Opening());

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Alert_title_falls_back_to_the_alert_name()
    {
        TestData.Alert(summary: null).Title.Value.Should().Be("ApiHighErrorRate");
    }

    [Fact]
    public void Repeated_firing_records_an_alert_only()
    {
        var incident = TestData.AlertIncident();
        incident.ClearDomainEvents();

        incident.RecordAlert(TestData.Alert(), TestData.Now.AddMinutes(5));

        incident.Timeline[^1].Kind.Should().Be(TimelineKind.Alert);
        incident.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<AlertRecorded>();
        incident.Status.Should().Be(IncidentStatus.Triggered);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolved_alert_mitigates_triggered_or_acknowledged_incidents(bool acknowledged)
    {
        var incident = TestData.AlertIncident();
        if (acknowledged)
        {
            incident.Acknowledge(TestData.Ava, TestData.Now.AddMinutes(2));
        }

        incident.ClearDomainEvents();

        incident.RecordAlert(TestData.Alert(AlertStatus.Resolved), TestData.Now.AddMinutes(10));

        incident.DomainEvents.Select(domainEvent => domainEvent.GetType()).Should().Equal(typeof(AlertRecorded), typeof(IncidentMitigated));
        incident.Timeline[^1].Actor.Should().Be(Actor.Alerting);
        incident.Status.Should().Be(IncidentStatus.Mitigated);
        incident.MitigatedAt.Should().Be(TestData.Now.AddMinutes(10));
        incident.RootCause.Should().BeNull();
    }

    [Fact]
    public void Resolved_alert_on_mitigated_incident_only_records()
    {
        var incident = TestData.AlertIncident();
        incident.Mitigate(TestData.Ava, TestData.Note("Rolled back"), TestData.Now.AddMinutes(4));

        incident.RecordAlert(TestData.Alert(AlertStatus.Resolved), TestData.Now.AddMinutes(10));

        incident.Timeline[^1].Kind.Should().Be(TimelineKind.Alert);
        incident.MitigatedAt.Should().Be(TestData.Now.AddMinutes(4));
    }

    [Fact]
    public void Alert_with_another_fingerprint_is_rejected()
    {
        var incident = TestData.AlertIncident();

        var act = () => incident.RecordAlert(TestData.Alert(fingerprint: "other"), TestData.Now);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Manual_source_is_not_an_alert()
    {
        var act = () => Alert.From(new AlertSignal(IncidentSource.Manual, new AlertFingerprint("fp"), AlertStatus.Firing, "Name", null, null, null, Severity.Sev3));

        act.Should().Throw<DomainValidationException>();
    }
}
