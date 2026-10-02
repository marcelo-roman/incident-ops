using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents;
using IncidentOps.Domain.Errors;
using Microsoft.Extensions.Options;

namespace IncidentOps.Api.Demo;

internal sealed partial class DemoTrafficService(
    IServiceScopeFactory scopes,
    IOptions<DemoTrafficOptions> options,
    ILogger<DemoTrafficService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        LogStarted(options.Value.IntervalSeconds);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TickAsync(stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DemoTrafficScenario>().RunOnceAsync(stoppingToken);
        }
        catch (Exception exception) when (exception is DomainException or ConcurrencyConflictException or IncidentNotFoundException)
        {
            LogSkipped(exception.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo traffic enabled every {IntervalSeconds}s")]
    private partial void LogStarted(int intervalSeconds);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Demo traffic step skipped: {Reason}")]
    private partial void LogSkipped(string reason);
}
