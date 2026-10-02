namespace IncidentOps.Domain.Errors;

public abstract class DomainException(string message) : Exception(message);
