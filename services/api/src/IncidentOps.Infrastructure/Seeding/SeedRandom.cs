namespace IncidentOps.Infrastructure.Seeding;

internal sealed class SeedRandom(int seed)
{
    private readonly Random _random = new(seed);

    public double NextDouble() => _random.NextDouble();

    public int Between(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    public double Between(double min, double max) => min + ((max - min) * _random.NextDouble());

    public bool Chance(double probability) => _random.NextDouble() < probability;

    public T Pick<T>(IReadOnlyList<T> items) => items[_random.Next(items.Count)];

    public T Weighted<T>(IEnumerable<KeyValuePair<T, int>> weights)
    {
        var options = weights.Where(option => option.Value > 0).ToList();
        var roll = _random.Next(options.Sum(option => option.Value));
        foreach (var option in options)
        {
            if (roll < option.Value)
            {
                return option.Key;
            }

            roll -= option.Value;
        }

        return options[^1].Key;
    }

    public int Poisson(double mean)
    {
        var limit = Math.Exp(-mean);
        var count = 0;
        var product = _random.NextDouble();
        while (product > limit)
        {
            count++;
            product *= _random.NextDouble();
        }

        return count;
    }
}
