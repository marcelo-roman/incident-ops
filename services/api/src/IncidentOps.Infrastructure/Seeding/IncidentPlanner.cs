using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Infrastructure.Seeding;

internal sealed class IncidentPlanner(SeedRandom random)
{
    private const double WeekdayRate = 2.4;
    private const double WeekendRate = 1.1;
    private const double BusinessHoursShare = 0.65;

    private static readonly IReadOnlyList<IncidentBurst> Bursts =
    [
        new(IncidentThemeCatalog.PaymentProviderTimeout, "payments-gateway", 121, 4, 4),
        new(IncidentThemeCatalog.QueueBacklog, "notifications", 58, 6, 3),
        new(IncidentThemeCatalog.MemoryLeakAfterDeploy, "search", 27, 3, 3),
    ];

    public IReadOnlyList<IncidentPlan> Plan(DateTimeOffset firstDay, int days, DateTimeOffset now)
    {
        var plans = new List<IncidentPlan>();
        for (var day = 0; day < days; day++)
        {
            var date = firstDay.AddDays(day);
            plans.AddRange(PlanDay(date, days - day));
        }

        return plans
            .Where(plan => plan.CreatedAt < now)
            .OrderBy(plan => plan.CreatedAt)
            .ToList();
    }

    private IEnumerable<IncidentPlan> PlanDay(DateTimeOffset date, int daysAgo)
    {
        var count = random.Poisson(DailyRate(date));
        for (var index = 0; index < count; index++)
        {
            var theme = random.Weighted(IncidentThemeCatalog.All.Select(item => KeyValuePair.Create(item, item.Weight)));
            yield return PlanOne(date, theme, random.Pick(theme.ServiceIds));
        }

        foreach (var burst in Bursts.Where(burst => burst.Covers(daysAgo)))
        {
            for (var index = 0; index < burst.PerDay; index++)
            {
                yield return PlanOne(date, burst.Theme, burst.ServiceId);
            }
        }
    }

    private IncidentPlan PlanOne(DateTimeOffset date, IncidentTheme theme, string serviceId)
    {
        var draft = new IncidentDraft(
            new IncidentTitle(random.Pick(theme.Titles)),
            new Description(random.Pick(theme.Descriptions)),
            new ServiceId(serviceId),
            random.Weighted(theme.SeverityWeights));

        return new IncidentPlan(date + TimeOfDay(), theme, draft);
    }

    private TimeSpan TimeOfDay()
    {
        var minute = random.Between(0, 60);
        if (random.Chance(BusinessHoursShare))
        {
            return new TimeSpan(random.Between(13, 22), minute, 0);
        }

        return new TimeSpan(random.Between(0, 24), minute, 0);
    }

    private static double DailyRate(DateTimeOffset date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return WeekendRate;
        }

        return WeekdayRate;
    }
}
