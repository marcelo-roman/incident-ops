using IncidentOps.Api.Errors;
using IncidentOps.Api.Hosting;
using IncidentOps.Api.Security;
using IncidentOps.Application.Alerts;
using IncidentOps.Application.Alerts.Alertmanager;
using IncidentOps.Application.Alerts.AzureMonitor;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IncidentOps.Api.Alerts;

internal static class AlertEndpoints
{
    public static RouteGroupBuilder MapAlertEndpoints(this RouteGroupBuilder api)
    {
        var alerts = api.MapGroup("/alerts")
            .WithTags("Alerts")
            .RequireApiKey()
            .RequireRateLimiting(RateLimitingRegistration.WritesPolicy);

        alerts.MapPost("/alertmanager", IngestAlertmanager).WithName("IngestAlertmanagerAlerts").WithAlertProblems();
        alerts.MapPost("/azure-monitor", IngestAzureMonitor).WithName("IngestAzureMonitorAlert").WithAlertProblems();
        return api;
    }

    private static async Task<Ok<AlertIngestionResult>> IngestAlertmanager(
        AlertmanagerWebhook webhook,
        IngestAlertmanagerAlertsHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(webhook, cancellationToken));

    private static async Task<Ok<AlertIngestionResult>> IngestAzureMonitor(
        AzureMonitorAlert alert,
        IngestAzureMonitorAlertHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(alert, cancellationToken));
}
