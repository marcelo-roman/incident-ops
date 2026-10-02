using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Infrastructure.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IncidentOps.Escalation.Infrastructure.Paging;

public static class PagingRegistration
{
    public static IServiceCollection AddLogicAppPaging(this IServiceCollection services)
    {
        services.AddOptions<NotificationOptions>()
            .BindConfiguration(NotificationOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<WebOptions>()
            .BindConfiguration(WebOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(CreateLinks);
        services.AddSingleton<PagePayloadTranslator>();
        services.AddHttpClient<IPager, LogicAppPager>()
            .RemoveAllLoggers()
            .AddEscalationResilience();
        return services;
    }

    private static IncidentLinks CreateLinks(IServiceProvider provider)
    {
        var webBaseUrl = provider.GetRequiredService<IOptions<WebOptions>>().Value.BaseUrl;
        return new IncidentLinks(webBaseUrl ?? throw new InvalidOperationException("Web:BaseUrl is not configured."));
    }
}
