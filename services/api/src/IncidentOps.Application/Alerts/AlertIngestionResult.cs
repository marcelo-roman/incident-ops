namespace IncidentOps.Application.Alerts;

public sealed record AlertIngestionResult(IReadOnlyList<AlertOutcome> Alerts);
