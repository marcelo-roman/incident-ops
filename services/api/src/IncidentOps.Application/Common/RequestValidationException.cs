namespace IncidentOps.Application.Common;

public sealed class RequestValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
