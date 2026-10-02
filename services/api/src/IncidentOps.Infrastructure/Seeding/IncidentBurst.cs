namespace IncidentOps.Infrastructure.Seeding;

internal sealed record IncidentBurst(IncidentTheme Theme, string ServiceId, int StartsDaysAgo, int Days, int PerDay)
{
    public bool Covers(int daysAgo) => daysAgo <= StartsDaysAgo && daysAgo > StartsDaysAgo - Days;
}
