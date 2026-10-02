using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IncidentOps.Api.Tests.Infrastructure;

public static class TestTokens
{
    public const string Username = "demo";

    public static string Create(
        string signingKey = ApiFactory.SigningKey,
        string issuer = "incident-ops-api",
        string audience = "incident-ops",
        DateTime? expires = null)
    {
        var expiresAt = expires ?? DateTime.UtcNow.AddHours(1);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = expiresAt.AddHours(-2),
            NotBefore = expiresAt.AddHours(-2),
            Expires = expiresAt,
            Subject = new ClaimsIdentity([new Claim("sub", Username), new Claim("name", Username)]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256),
        });
    }
}
