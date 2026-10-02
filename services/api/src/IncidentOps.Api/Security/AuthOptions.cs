using System.Text;

namespace IncidentOps.Api.Security;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public const string Issuer = "incident-ops-api";
    public const string Audience = "incident-ops";
    public const int MinimumSigningKeyBytes = 32;

    public string SigningKey { get; set; } = string.Empty;

    public string DemoUsername { get; set; } = string.Empty;

    public string DemoPassword { get; set; } = string.Empty;

    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(8);

    public bool HasValidSigningKey() => Encoding.UTF8.GetByteCount(SigningKey) >= MinimumSigningKeyBytes;
}
