namespace IncidentOps.Application.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
