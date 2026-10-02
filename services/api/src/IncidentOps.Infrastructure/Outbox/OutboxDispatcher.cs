using IncidentOps.Application.Common;
using IncidentOps.Application.Common.Outbox;
using IncidentOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IncidentOps.Infrastructure.Outbox;

internal sealed partial class OutboxDispatcher(
    IServiceScopeFactory scopes,
    OutboxSignal signal,
    IClock clock,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private readonly OutboxRetryPolicy _retryPolicy = new(
        options.Value.MaxAttempts,
        TimeSpan.FromSeconds(options.Value.MaxRetryDelaySeconds));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(options.Value.PollingIntervalMilliseconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            var dispatched = await DispatchSafelyAsync(stoppingToken);
            if (dispatched < options.Value.BatchSize)
            {
                await signal.WaitAsync(interval, stoppingToken);
            }
        }
    }

    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IncidentOpsDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<IOutboxMessageHandler>();
        var now = clock.UtcNow;
        var pending = await db.OutboxMessages
            .Where(message => message.ProcessedAt == null && message.DeadLetteredAt == null && message.NextAttemptAt <= now)
            .OrderBy(message => message.Sequence)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            await DispatchAsync(message, handler, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return pending.Count;
    }

    private async Task<int> DispatchSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            return await DispatchPendingAsync(stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogBatchFailed(exception);
            return 0;
        }
    }

    private async Task DispatchAsync(OutboxMessage message, IOutboxMessageHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            await handler.HandleAsync(message.Type, message.Payload, cancellationToken);
            message.MarkProcessed(clock.UtcNow);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            message.MarkFailed(exception.Message, clock.UtcNow, _retryPolicy);
            LogMessageFailed(exception, message.Id, message.Attempts);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} failed on attempt {Attempt}")]
    private partial void LogMessageFailed(Exception exception, Guid messageId, int attempt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox dispatch batch failed")]
    private partial void LogBatchFailed(Exception exception);
}
