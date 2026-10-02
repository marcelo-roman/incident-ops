using Microsoft.Extensions.Options;

namespace IncidentOps.Api.Security;

internal sealed class DemoAccount(IOptions<AuthOptions> options)
{
    public bool Verify(string? username, string? password)
    {
        var account = options.Value;
        var usernameMatches = SecretComparer.Matches(account.DemoUsername, username);
        var passwordMatches = SecretComparer.Matches(account.DemoPassword, password);
        return usernameMatches && passwordMatches;
    }
}
