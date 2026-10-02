namespace IncidentOps.Domain.Errors;

public sealed class EscalationNotAllowedException(string message) : DomainException(message);
