using IncidentOps.Application.Alerts;
using IncidentOps.Application.Alerts.Alertmanager;
using IncidentOps.Application.Alerts.AzureMonitor;
using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents.Messaging;
using IncidentOps.Application.Tests.Fakes;
using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Application.Tests;

public sealed class AlertIngestionUseCaseTests : IDisposable
{
    private readonly ApplicationHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task Firing_alert_opens_an_incident_with_the_mapped_fields()
    {
        var result = await IngestAsync(Webhook(AlertmanagerAlert("firing", "fp-1", "critical", "checkout")));
        await _harness.DispatchOutboxAsync();

        result.Alerts.Should().ContainSingle().Which.Action.Should().Be(AlertAction.OpenIncident);
        var incident = _harness.Incidents.All.Single();
        incident.Source.Should().Be(IncidentSource.Alertmanager);
        incident.AlertFingerprint!.Value.Should().Be("fp-1");
        incident.Severity.Should().Be(Severity.Sev1);
        incident.ServiceId.Value.Should().Be("checkout");
        incident.Title.Value.Should().Be("Error rate above 5%");
        _harness.Events.Types.Should().Equal(IncidentEventTypes.Triggered);
    }

    [Fact]
    public async Task Unknown_service_falls_back_to_platform()
    {
        await IngestAsync(Webhook(AlertmanagerAlert("firing", "fp-2", "warning", "Legacy Batch")));

        _harness.Incidents.All.Single().ServiceId.Value.Should().Be("platform");
    }

    [Fact]
    public async Task Repeated_firing_appends_to_the_open_incident_without_integration_events()
    {
        await IngestAsync(Webhook(AlertmanagerAlert("firing", "fp-3", "high", "checkout")));

        var result = await IngestAsync(Webhook(AlertmanagerAlert("firing", "fp-3", "high", "checkout")));
        await _harness.DispatchOutboxAsync();

        result.Alerts.Single().Action.Should().Be(AlertAction.AppendToIncident);
        _harness.Incidents.All.Should().ContainSingle()
            .Which.Timeline.Select(entry => entry.Kind).Should().Equal(TimelineKind.Triggered, TimelineKind.Alert);
        _harness.Events.Types.Should().Equal(IncidentEventTypes.Triggered);
        _harness.Notifier.Messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task Resolved_alert_mitigates_and_publishes()
    {
        await IngestAsync(Webhook(AlertmanagerAlert("firing", "fp-4", "critical", "checkout")));

        await IngestAsync(Webhook(AlertmanagerAlert("resolved", "fp-4", "critical", "checkout")));
        await _harness.DispatchOutboxAsync();

        _harness.Incidents.All.Single().Status.Should().Be(IncidentStatus.Mitigated);
        _harness.Events.Types.Should().Equal(IncidentEventTypes.Triggered, IncidentEventTypes.Mitigated);
    }

    [Fact]
    public async Task Resolved_alert_without_open_incident_is_ignored()
    {
        var result = await IngestAsync(Webhook(AlertmanagerAlert("resolved", "fp-5", "critical", "checkout")));

        result.Alerts.Single().Action.Should().Be(AlertAction.Ignore);
        _harness.Incidents.All.Should().BeEmpty();
        _harness.UnitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task Unsupported_version_and_status_are_rejected()
    {
        var version = () => IngestAsync(new AlertmanagerWebhook("3", null, "firing", "incident-ops", []));
        var status = () => IngestAsync(Webhook(AlertmanagerAlert("pending", "fp-6", "critical", "checkout")));

        (await version.Should().ThrowAsync<RequestValidationException>()).Which.Field.Should().Be("version");
        await status.Should().ThrowAsync<RequestValidationException>();
    }

    [Fact]
    public async Task Azure_monitor_alert_opens_and_resolves()
    {
        var handler = _harness.Get<IngestAzureMonitorAlertHandler>();

        var fired = await handler.HandleAsync(AzureAlert("Fired"), CancellationToken.None);
        var resolved = await handler.HandleAsync(AzureAlert("Resolved"), CancellationToken.None);

        fired.Alerts.Single().Fingerprint.Should().Be("api-failed-requests|/subscriptions/1/appi");
        resolved.Alerts.Single().Action.Should().Be(AlertAction.AppendToIncident);
        var incident = _harness.Incidents.All.Single();
        incident.Source.Should().Be(IncidentSource.AzureMonitor);
        incident.Severity.Should().Be(Severity.Sev2);
        incident.ServiceId.Value.Should().Be("identity");
        incident.Status.Should().Be(IncidentStatus.Mitigated);
    }

    [Fact]
    public async Task Azure_monitor_requires_the_common_alert_schema()
    {
        var act = () => _harness.Get<IngestAzureMonitorAlertHandler>().HandleAsync(
            new AzureMonitorAlert("Microsoft.Insights/activityLogs", null),
            CancellationToken.None);

        (await act.Should().ThrowAsync<RequestValidationException>()).Which.Field.Should().Be("schemaId");
    }

    private Task<AlertIngestionResult> IngestAsync(AlertmanagerWebhook webhook) =>
        _harness.Get<IngestAlertmanagerAlertsHandler>().HandleAsync(webhook, CancellationToken.None);

    private static AlertmanagerWebhook Webhook(params AlertmanagerAlert[] alerts) =>
        new(AlertmanagerTranslator.SupportedVersion, "{}:{}", "firing", "incident-ops", alerts);

    private static AlertmanagerAlert AlertmanagerAlert(string status, string fingerprint, string severity, string service) =>
        new(
            status,
            new Dictionary<string, string> { ["alertname"] = "ApiHighErrorRate", ["severity"] = severity, ["service"] = service },
            new Dictionary<string, string> { ["summary"] = "Error rate above 5%", ["description"] = "5xx ratio high" },
            fingerprint);

    private static AzureMonitorAlert AzureAlert(string monitorCondition) =>
        new(
            AzureMonitorTranslator.CommonAlertSchema,
            new AzureMonitorAlertData(
                new AzureMonitorEssentials("alert-1", "api-failed-requests", "Sev2", monitorCondition, "Failed requests above 5%", ["/subscriptions/1/appi"]),
                new Dictionary<string, string> { ["service"] = "identity" }));
}
