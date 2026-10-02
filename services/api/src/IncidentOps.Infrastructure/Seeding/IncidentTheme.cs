using IncidentOps.Domain.Incidents;

namespace IncidentOps.Infrastructure.Seeding;

public sealed record IncidentTheme(
    string Name,
    int Weight,
    IReadOnlyList<string> ServiceIds,
    IReadOnlyDictionary<Severity, int> SeverityWeights,
    IReadOnlyList<string> Titles,
    IReadOnlyList<string> Descriptions,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Mitigations,
    IReadOnlyList<string> RootCauses);
