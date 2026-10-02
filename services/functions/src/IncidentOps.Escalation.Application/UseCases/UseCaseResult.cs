namespace IncidentOps.Escalation.Application.UseCases;

public sealed record UseCaseResult(bool Acted, string Description)
{
    public static UseCaseResult Done(string description) => new(true, description);

    public static UseCaseResult Skipped(string reason) => new(false, reason);
}
