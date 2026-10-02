using System.Globalization;
using IncidentOps.Application.Common;
using IncidentOps.Domain.OnCall;
using IncidentOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IncidentOps.Infrastructure.Seeding;

public sealed partial class DatabaseInitializer(
    IncidentOpsDbContext db,
    IOnCallRotationRepository rotations,
    IClock clock,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (options.Value.ApplyMigrations)
        {
            await db.Database.MigrateAsync(cancellationToken);
            LogMigrated();
        }

        if (options.Value.Seed)
        {
            await SeedReferenceDataAsync(cancellationToken);
            await SeedHistoryAsync(cancellationToken);
        }
    }

    private async Task SeedReferenceDataAsync(CancellationToken cancellationToken)
    {
        var knownServices = await db.Services.Select(service => service.Id).ToListAsync(cancellationToken);
        db.Services.AddRange(ReferenceData.Services().Where(service => !knownServices.Contains(service.Id)));

        if (!await db.Engineers.AnyAsync(cancellationToken))
        {
            db.Engineers.AddRange(ReferenceData.Engineers());
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedHistoryAsync(CancellationToken cancellationToken)
    {
        if (await db.Incidents.AnyAsync(cancellationToken))
        {
            return;
        }

        var rotation = await rotations.GetAsync(cancellationToken);
        var incidents = new HistoricalIncidentGenerator(rotation).Generate(clock.UtcNow, IncidentOpsDbContext.FirstIncidentNumber);
        incidents.ToList().ForEach(incident => incident.ClearDomainEvents());
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            db.Incidents.AddRange(incidents);
            await db.SaveChangesAsync(cancellationToken);
            await RestartNumberSequenceAsync(IncidentOpsDbContext.FirstIncidentNumber + incidents.Count, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        LogSeeded(incidents.Count);
    }

    private Task<int> RestartNumberSequenceAsync(int nextNumber, CancellationToken cancellationToken)
    {
        var statement = string.Create(
            CultureInfo.InvariantCulture,
            $"ALTER SEQUENCE [dbo].[{IncidentOpsDbContext.IncidentNumberSequence}] RESTART WITH {nextNumber}");

        return db.Database.ExecuteSqlRawAsync(statement, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database migrations applied")]
    private partial void LogMigrated();

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {IncidentCount} historical incidents")]
    private partial void LogSeeded(int incidentCount);
}
