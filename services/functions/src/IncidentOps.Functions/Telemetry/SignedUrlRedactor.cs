using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace IncidentOps.Functions.Telemetry;

public sealed class SignedUrlRedactor : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is not DependencyTelemetry dependency)
        {
            return;
        }

        if (!Uri.TryCreate(dependency.Data, UriKind.Absolute, out var uri))
        {
            return;
        }

        if (!uri.Query.Contains("sig=", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        dependency.Data = uri.GetLeftPart(UriPartial.Path);
    }
}
