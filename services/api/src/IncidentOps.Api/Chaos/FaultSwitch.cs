namespace IncidentOps.Api.Chaos;

internal sealed class FaultSwitch
{
    private volatile ActiveFault? _fault;

    public ActiveFault? ActiveAt(DateTimeOffset now)
    {
        var fault = _fault;
        if (fault is null || fault.ExpiresAt <= now)
        {
            return null;
        }

        return fault;
    }

    public void Activate(ActiveFault fault) => _fault = fault;

    public void Clear() => _fault = null;
}
