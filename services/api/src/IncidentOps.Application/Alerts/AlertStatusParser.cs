using IncidentOps.Application.Common;
using IncidentOps.Domain.Alerts;

namespace IncidentOps.Application.Alerts;

internal static class AlertStatusParser
{
    public static AlertStatus Parse(string? value, string firing, string resolved, string field)
    {
        if (string.Equals(value, firing, StringComparison.OrdinalIgnoreCase))
        {
            return AlertStatus.Firing;
        }

        if (string.Equals(value, resolved, StringComparison.OrdinalIgnoreCase))
        {
            return AlertStatus.Resolved;
        }

        throw new RequestValidationException(field, $"'{value}' is not a supported alert status.");
    }
}
