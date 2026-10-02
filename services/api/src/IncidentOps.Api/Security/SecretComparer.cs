using System.Security.Cryptography;
using System.Text;

namespace IncidentOps.Api.Security;

internal static class SecretComparer
{
    public static bool Matches(string? expected, string? provided)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(provided))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Digest(expected), Digest(provided));
    }

    private static byte[] Digest(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
