namespace IncidentOps.Api.Hosting;

internal static class CorsRegistration
{
    public const string SectionName = "Cors:AllowedOrigins";

    private static readonly string[] DefaultOrigins = ["https://incidents.marceloroman.com.br", "http://localhost:5173"];

    public static IServiceCollection AddConsoleCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection(SectionName).Get<string[]>() ?? DefaultOrigins;
        return services.AddCors(cors => cors.AddDefaultPolicy(policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));
    }
}
