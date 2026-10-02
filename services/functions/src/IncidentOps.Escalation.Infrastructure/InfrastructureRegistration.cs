using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Infrastructure.AntiCorruption;
using IncidentOps.Escalation.Infrastructure.IncidentsApi;
using IncidentOps.Escalation.Infrastructure.Paging;
using IncidentOps.Escalation.Infrastructure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentOps.Escalation.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddEscalationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IIncidentEventTranslator, CloudEventIncidentTranslator>();
        services.AddSingleton<IAcknowledgementCheckTranslator, SlaCheckMessageTranslator>();
        services.AddSlaCheckScheduling(configuration);
        services.AddIncidentsApi();
        services.AddLogicAppPaging();
        return services;
    }
}
