namespace IncidentOps.Domain.Common;

internal static class Text
{
    public static string FirstNonBlank(string? preferred, string fallback)
    {
        if (string.IsNullOrWhiteSpace(preferred))
        {
            return fallback;
        }

        return preferred.Trim();
    }

    public static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
