namespace IncidentOps.Escalation.Infrastructure.AntiCorruption;

public sealed class UpstreamContractException : Exception
{
    public UpstreamContractException()
    {
    }

    public UpstreamContractException(string message)
        : base(message)
    {
    }

    public UpstreamContractException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
