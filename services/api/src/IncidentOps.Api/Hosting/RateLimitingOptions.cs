namespace IncidentOps.Api.Hosting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int WritePermitLimit { get; set; } = 30;

    public int WindowSeconds { get; set; } = 60;
}
