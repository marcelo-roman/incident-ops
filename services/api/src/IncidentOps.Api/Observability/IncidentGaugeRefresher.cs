using IncidentOps.Application.Metrics;
using Microsoft.Extensions.Options;

namespace IncidentOps.Api.Observability;

internal sealed partial class IncidentGaugeRefresher(
    IServiceScopeFactory scopes,
    IncidentOpsMetrics metrics,
    IOptions<ObservabilityOptions> options,
    ILogger<IncidentGaugeRefresher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.GaugeRefreshSeconds));
        do
        {
            await RefreshAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<GetMetricsSummaryHandler>();
            metrics.UpdateSummary(await handler.HandleAsync(stoppingToken));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogRefreshFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not refresh incident gauges")]
    private partial void LogRefreshFailed(Exception exception);
}
