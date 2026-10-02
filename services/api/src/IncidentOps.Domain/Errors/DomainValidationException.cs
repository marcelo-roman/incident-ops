namespace IncidentOps.Domain.Errors;

public sealed class DomainValidationException(string field, string message) : DomainException(message)
{
    public string Field { get; } = field;
}
