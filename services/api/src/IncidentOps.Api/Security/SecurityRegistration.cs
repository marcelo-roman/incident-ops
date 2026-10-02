using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace IncidentOps.Api.Security;

internal static class SecurityRegistration
{
    public static IServiceCollection AddIncidentOpsSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiKeyOptions>(configuration.GetSection(ApiKeyOptions.SectionName));
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .Validate(options => options.HasValidSigningKey(), $"Auth:SigningKey must be at least {AuthOptions.MinimumSigningKeyBytes} bytes.")
            .Validate(options => options.TokenLifetime > TimeSpan.Zero, "Auth:TokenLifetime must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<DemoAccount>();
        services.AddSingleton<AccessTokenIssuer>();
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerConfiguration>();

        services
            .AddAuthentication(AuthenticationSchemes.BearerOrApiKey)
            .AddPolicyScheme(AuthenticationSchemes.BearerOrApiKey, AuthenticationSchemes.BearerOrApiKey, options =>
                options.ForwardDefaultSelector = CallerSchemeSelector.Select)
            .AddJwtBearer(AuthenticationSchemes.Bearer)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(AuthenticationSchemes.ApiKey, configureOptions: null);

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(AuthorizationPolicies.AnyCaller)
            .SetFallbackPolicy(AuthorizationPolicies.AnyCaller)
            .AddPolicy(AuthorizationPolicies.ServiceApiKey, AuthorizationPolicies.ServiceOnly);

        return services;
    }
}
