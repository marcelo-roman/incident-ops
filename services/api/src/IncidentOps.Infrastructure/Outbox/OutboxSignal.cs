namespace IncidentOps.Infrastructure.Outbox;

internal sealed class OutboxSignal : IDisposable
{
    private readonly SemaphoreSlim _pending = new(0, 1);

    public void Notify()
    {
        if (_pending.CurrentCount > 0)
        {
            return;
        }

        try
        {
            _pending.Release();
        }
        catch (SemaphoreFullException)
        {
            return;
        }
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        _pending.WaitAsync(timeout, cancellationToken);

    public void Dispose() => _pending.Dispose();
}
