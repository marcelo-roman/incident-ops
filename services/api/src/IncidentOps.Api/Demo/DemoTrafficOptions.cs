namespace IncidentOps.Api.Demo;

public sealed class DemoTrafficOptions
{
    public const string SectionName = "DemoTraffic";

    public bool Enabled { get; set; }

    public int IntervalSeconds { get; set; } = 45;

    public int TargetOpenIncidents { get; set; } = 4;
}
