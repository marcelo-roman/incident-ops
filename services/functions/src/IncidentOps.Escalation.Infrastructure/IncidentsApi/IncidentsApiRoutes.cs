using System.Globalization;
using IncidentOps.Escalation.Domain.Incidents;

namespace IncidentOps.Escalation.Infrastructure.IncidentsApi;

public static class IncidentsApiRoutes
{
    public static Uri CurrentOnCall { get; } = new("api/oncall/current", UriKind.Relative);

    public static Uri Incident(IncidentId incidentId) => Relative($"api/incidents/{incidentId.Value}");

    public static Uri Escalate(IncidentId incidentId) => Relative($"api/incidents/{incidentId.Value}/escalate");

    private static Uri Relative(FormattableString path) =>
        new(path.ToString(CultureInfo.InvariantCulture), UriKind.Relative);
}
