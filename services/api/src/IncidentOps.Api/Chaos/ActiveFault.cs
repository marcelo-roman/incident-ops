namespace IncidentOps.Api.Chaos;

public sealed record ActiveFault(double ErrorRate, int LatencyMs, DateTimeOffset ExpiresAt);
