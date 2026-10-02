using IncidentOps.Application.Alerts.Alertmanager;
using IncidentOps.Application.Alerts.AzureMonitor;
using IncidentOps.Application.Alerts.Ingest;
using IncidentOps.Application.Catalog;
using IncidentOps.Application.Common.Outbox;
using IncidentOps.Application.Incidents;
using IncidentOps.Application.Incidents.Acknowledge;
using IncidentOps.Application.Incidents.Details;
using IncidentOps.Application.Incidents.Escalate;
using IncidentOps.Application.Incidents.Export;
using IncidentOps.Application.Incidents.Listing;
using IncidentOps.Application.Incidents.Messaging;
using IncidentOps.Application.Incidents.Mitigate;
using IncidentOps.Application.Incidents.Notes;
using IncidentOps.Application.Incidents.Resolve;
using IncidentOps.Application.Incidents.Trigger;
using IncidentOps.Application.Metrics;
using IncidentOps.Application.OnCall;
using IncidentOps.Application.Sla;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentOps.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SlaTargetsResolver>();
        services.AddScoped<IncidentOpenings>();
        services.AddSingleton<IDomainEventTranslator, IncidentEventTranslator>();
        services.AddScoped<IOutboxMessageHandler, IncidentChangePublisher>();

        services.AddScoped<TriggerIncidentHandler>();
        services.AddScoped<AcknowledgeIncidentHandler>();
        services.AddScoped<EscalateIncidentHandler>();
        services.AddScoped<MitigateIncidentHandler>();
        services.AddScoped<ResolveIncidentHandler>();
        services.AddScoped<AddIncidentNoteHandler>();
        services.AddScoped<IngestAlertHandler>();
        services.AddScoped<AlertBatchIngestion>();
        services.AddScoped<IngestAlertmanagerAlertsHandler>();
        services.AddScoped<IngestAzureMonitorAlertHandler>();

        services.AddScoped<GetIncidentHandler>();
        services.AddScoped<ListIncidentsHandler>();
        services.AddScoped<ExportIncidentsHandler>();
        services.AddScoped<ListServicesHandler>();
        services.AddScoped<GetCurrentOnCallHandler>();
        services.AddScoped<GetMetricsSummaryHandler>();
        return services;
    }
}
