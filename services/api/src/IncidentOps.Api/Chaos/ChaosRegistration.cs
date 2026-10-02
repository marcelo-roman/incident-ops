namespace IncidentOps.Api.Chaos;

internal static class ChaosRegistration
{
    public static bool IsChaosEnabled(this IConfiguration configuration) =>
        configuration.GetSection(ChaosOptions.SectionName).Get<ChaosOptions>()?.Enabled ?? false;

    public static IServiceCollection AddChaos(this IServiceCollection services) =>
        services.AddSingleton<FaultSwitch>();
}
