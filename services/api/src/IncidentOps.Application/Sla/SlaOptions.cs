namespace IncidentOps.Application.Sla;

public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    public double TimeScale { get; set; } = 1.0;
}
