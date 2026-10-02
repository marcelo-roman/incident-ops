using IncidentOps.Escalation.Application.UseCases;
using IncidentOps.Escalation.Infrastructure;
using IncidentOps.Functions.Messaging;
using IncidentOps.Functions.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentOps.Functions.Composition;

public static class IncidentOpsServices
{
    public static IServiceCollection AddIncidentOps(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIncidentOpsTelemetry();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<MessageSettlement>();
        services.AddScoped<ScheduleAcknowledgementCheck>();
        services.AddScoped<CheckAcknowledgementSla>();
        services.AddScoped<PageOnCall>();
        services.AddEscalationInfrastructure(configuration);
        return services;
    }
}
