using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.OnCall;

namespace IncidentOps.Infrastructure.Seeding;

public sealed class HistoricalIncidentGenerator(OnCallRotation rotation, int seed = HistoricalIncidentGenerator.DefaultSeed)
{
    public const int DefaultSeed = 1042;
    public const int Days = 182;

    public IReadOnlyList<Incident> Generate(DateTimeOffset now, int firstNumber)
    {
        var random = new SeedRandom(seed);
        var firstDay = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero).AddDays(-Days + 1);
        var plans = new IncidentPlanner(random).Plan(firstDay, Days, now);
        var simulator = new IncidentSimulator(random, rotation);
        return plans
            .Select((plan, index) => simulator.Simulate(plan, firstNumber + index, now))
            .ToList();
    }
}
