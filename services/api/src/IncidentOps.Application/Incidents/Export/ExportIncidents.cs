using IncidentOps.Application.Common;

namespace IncidentOps.Application.Incidents.Export;

public sealed record ExportIncidents(DateTimeOffset? From, DateTimeOffset? To)
{
    public static readonly TimeSpan DefaultRange = TimeSpan.FromDays(180);
    public static readonly TimeSpan MaxRange = TimeSpan.FromDays(400);

    public (DateTimeOffset From, DateTimeOffset To) RangeEndingAt(DateTimeOffset now)
    {
        var to = To ?? now;
        var from = From ?? to - DefaultRange;
        if (from > to)
        {
            throw new RequestValidationException("from", "'from' must not be after 'to'.");
        }

        if (to - from > MaxRange)
        {
            throw new RequestValidationException("from", $"The export range must not exceed {MaxRange.TotalDays} days.");
        }

        return (from, to);
    }
}
