using System.Globalization;
using IncidentOps.Escalation.Domain.Incidents;

namespace IncidentOps.Escalation.Infrastructure.Paging;

public sealed class IncidentLinks(Uri webBaseUrl)
{
    public Uri For(IncidentId incidentId) =>
        new(webBaseUrl, string.Create(CultureInfo.InvariantCulture, $"incidents/{incidentId.Value}"));
}
