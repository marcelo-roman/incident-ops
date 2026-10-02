namespace IncidentOps.Escalation.Domain.Incidents;

public sealed record Severity
{
    public static readonly Severity Sev1 = new("Sev1", 1);
    public static readonly Severity Sev2 = new("Sev2", 2);
    public static readonly Severity Sev3 = new("Sev3", 3);
    public static readonly Severity Sev4 = new("Sev4", 4);

    private const int LowestPagingRank = 2;

    private static readonly Severity[] All = [Sev1, Sev2, Sev3, Sev4];

    private Severity(string name, int rank)
    {
        Name = name;
        Rank = rank;
    }

    public string Name { get; }

    public int Rank { get; }

    public bool PagesOnCall => Rank <= LowestPagingRank;

    public static Severity Parse(string? name) =>
        All.FirstOrDefault(severity => severity.Name == name)
            ?? throw new DomainException($"Unknown severity '{name}'.");

    public override string ToString() => Name;
}
