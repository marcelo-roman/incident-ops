using IncidentOps.Api.Security;
using Microsoft.OpenApi.Models;

namespace IncidentOps.Api.Hosting;

internal static class OpenApiRegistration
{
    public static IServiceCollection AddIncidentOpsOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        return services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Incident Ops API", Version = "v1" });
            options.AddSecurityDefinition(AuthenticationSchemes.Bearer, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Access token from POST /api/auth/token.",
            });
            options.AddSecurityDefinition(AuthenticationSchemes.ApiKey, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = ApiKeyReader.HeaderName,
                Description = "Service API key.",
            });
            options.AddSecurityRequirement(Requirement(AuthenticationSchemes.Bearer));
            options.AddSecurityRequirement(Requirement(AuthenticationSchemes.ApiKey));
        });
    }

    private static OpenApiSecurityRequirement Requirement(string scheme) => new()
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = scheme } }] = [],
    };
}
