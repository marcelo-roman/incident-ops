using IncidentOps.Escalation.Infrastructure.IncidentsApi;
using IncidentOps.Escalation.Infrastructure.Paging;
using IncidentOps.Functions.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentOps.Functions.Tests.Adapters;

internal static class ServiceProviders
{
    public static readonly Dictionary<string, string?> Settings = new()
    {
        ["ServiceBusConnection"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
        ["SlaChecksQueue"] = "sla-checks",
        ["IncidentsApi:BaseUrl"] = "https://incidents-api.example.com/",
        ["IncidentsApi:ApiKey"] = "test-key",
        ["Notifications:LogicAppUrl"] = "https://logic.example.com/workflows/notify/invoke?sig=secret",
        ["Web:BaseUrl"] = "https://incidents.example.com/",
    };

    public static IConfiguration Configuration(Dictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

    public static ServiceProvider WithHttpHandler(RecordingHttpHandler handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Configuration(Settings));
        services.AddIncidentsApi();
        services.AddLogicAppPaging();
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler));
        return services.BuildServiceProvider();
    }
}
