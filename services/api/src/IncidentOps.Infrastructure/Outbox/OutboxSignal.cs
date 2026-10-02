namespace IncidentOps.Infrastructure.Outbox;

internal sealed class OutboxSignal : IDisposable
{
    private readonly SemaphoreSlim _pending = new(0, 1);
    private readonly object _gate = new();

    public void Notify()
    {
        lock (_gate)
        {
            if (_pending.CurrentCount > 0)
            {
                return;
            }

            _pending.Release();
        }
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        _pending.WaitAsync(timeout, cancellationToken);

    public void Dispose() => _pending.Dispose();
}
