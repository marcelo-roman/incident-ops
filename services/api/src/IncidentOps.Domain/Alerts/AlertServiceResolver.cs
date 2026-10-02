using IncidentOps.Domain.Catalog;

namespace IncidentOps.Domain.Alerts;

public static class AlertServiceResolver
{
    public static ServiceId Resolve(string? serviceLabel, IEnumerable<ServiceId> knownServices)
    {
        if (string.IsNullOrWhiteSpace(serviceLabel))
        {
            return ServiceId.Platform;
        }

        var label = serviceLabel.Trim();
        var match = knownServices.FirstOrDefault(service => string.Equals(service.Value, label, StringComparison.OrdinalIgnoreCase));
        return match ?? ServiceId.Platform;
    }
}
