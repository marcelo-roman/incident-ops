using IncidentOps.Api.Security;
using Microsoft.OpenApi.Models;

namespace IncidentOps.Api.Hosting;

internal static class OpenApiRegistration
{
    private const string ApiKeyScheme = "ApiKey";

    public static IServiceCollection AddIncidentOpsOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        return services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Incident Ops API", Version = "v1" });
            options.AddSecurityDefinition(ApiKeyScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = ApiKeyReader.HeaderName,
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = ApiKeyScheme } }] = [],
            });
        });
    }
}
