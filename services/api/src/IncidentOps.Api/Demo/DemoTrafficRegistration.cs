namespace IncidentOps.Api.Demo;

internal static class DemoTrafficRegistration
{
    public static IServiceCollection AddDemoTraffic(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DemoTrafficOptions>(configuration.GetSection(DemoTrafficOptions.SectionName));
        services.AddScoped<DemoTrafficScenario>();
        return services.AddHostedService<DemoTrafficService>();
    }
}
