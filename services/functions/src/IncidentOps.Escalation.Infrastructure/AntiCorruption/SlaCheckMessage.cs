namespace IncidentOps.Escalation.Infrastructure.AntiCorruption;

public sealed record SlaCheckMessage(Guid IncidentId, int EscalationLevel);
