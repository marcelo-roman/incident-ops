using IncidentOps.Application.Common;

namespace IncidentOps.Api.Chaos;

public sealed record FaultInjectionRequest(double ErrorRate, int LatencyMs, int DurationSeconds)
{
    public const int MaxLatencyMs = 30_000;
    public const int MaxDurationSeconds = 3_600;

    public ActiveFault Activate(DateTimeOffset now)
    {
        Ensure(ErrorRate is >= 0 and <= 1, "errorRate", "'errorRate' must be between 0 and 1.");
        Ensure(LatencyMs is >= 0 and <= MaxLatencyMs, "latencyMs", $"'latencyMs' must be between 0 and {MaxLatencyMs}.");
        Ensure(DurationSeconds is >= 1 and <= MaxDurationSeconds, "durationSeconds", $"'durationSeconds' must be between 1 and {MaxDurationSeconds}.");
        return new ActiveFault(ErrorRate, LatencyMs, now.AddSeconds(DurationSeconds));
    }

    private static void Ensure(bool condition, string field, string message)
    {
        if (condition)
        {
            return;
        }

        throw new RequestValidationException(field, message);
    }
}
