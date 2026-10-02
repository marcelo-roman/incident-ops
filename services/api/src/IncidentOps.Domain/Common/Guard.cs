using IncidentOps.Domain.Errors;

namespace IncidentOps.Domain.Common;

internal static class Guard
{
    public static string Required(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(field, $"'{field}' is required.");
        }

        return Bounded(value, field, maxLength);
    }

    public static string Optional(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Bounded(value, field, maxLength);
    }

    public static TEnum Defined<TEnum>(TEnum value, string field)
        where TEnum : struct, Enum
    {
        if (Enum.IsDefined(value))
        {
            return value;
        }

        throw new DomainValidationException(field, $"'{field}' has an unsupported value.");
    }

    public static void Against(bool violated, string field, string message)
    {
        if (!violated)
        {
            return;
        }

        throw new DomainValidationException(field, message);
    }

    private static string Bounded(string value, string field, int maxLength)
    {
        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        throw new DomainValidationException(field, $"'{field}' must be at most {maxLength} characters.");
    }
}
