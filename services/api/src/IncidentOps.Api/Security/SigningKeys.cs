using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace IncidentOps.Api.Security;

internal static class SigningKeys
{
    public static SymmetricSecurityKey From(AuthOptions options) => new(Encoding.UTF8.GetBytes(options.SigningKey));
}
