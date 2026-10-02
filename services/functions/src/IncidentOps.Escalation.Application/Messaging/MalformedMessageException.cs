namespace IncidentOps.Escalation.Application.Messaging;

public sealed class MalformedMessageException : Exception
{
    public MalformedMessageException()
    {
    }

    public MalformedMessageException(string message)
        : base(message)
    {
    }

    public MalformedMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
