namespace IncidentOps.Api.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public int GaugeRefreshSeconds { get; set; } = 30;
}
