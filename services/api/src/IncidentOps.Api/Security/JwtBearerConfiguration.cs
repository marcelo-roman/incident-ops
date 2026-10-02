using IncidentOps.Api.RealTime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IncidentOps.Api.Security;

internal sealed class JwtBearerConfiguration(IOptions<AuthOptions> authOptions) : IConfigureNamedOptions<JwtBearerOptions>
{
    private const string AccessTokenQueryName = "access_token";
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != AuthenticationSchemes.Bearer)
        {
            return;
        }

        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = AuthOptions.Issuer,
            ValidAudience = AuthOptions.Audience,
            IssuerSigningKey = SigningKeys.From(authOptions.Value),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = ClockSkew,
            NameClaimType = JwtRegisteredClaimNames.Name,
        };
        options.Events = new JwtBearerEvents { OnMessageReceived = ReadHubAccessToken };
    }

    private static Task ReadHubAccessToken(MessageReceivedContext context)
    {
        if (!context.Request.Path.StartsWithSegments(IncidentsHub.Path))
        {
            return Task.CompletedTask;
        }

        var token = context.Request.Query[AccessTokenQueryName].FirstOrDefault();
        if (!string.IsNullOrEmpty(token))
        {
            context.Token = token;
        }

        return Task.CompletedTask;
    }
}
