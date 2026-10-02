using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.OnCall;
using IncidentOps.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace IncidentOps.Infrastructure.Persistence;

public sealed class IncidentOpsDbContext(DbContextOptions<IncidentOpsDbContext> options) : DbContext(options)
{
    public const string IncidentNumberSequence = "IncidentNumbers";
    public const int FirstIncidentNumber = 1001;

    public DbSet<Service> Services => Set<Service>();

    public DbSet<Engineer> Engineers => Set<Engineer>();

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<TimelineEntry> TimelineEntries => Set<TimelineEntry>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>(IncidentNumberSequence).StartsAt(FirstIncidentNumber);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(IncidentOpsDbContext).Assembly,
            type => type.Namespace != typeof(ReadModel.IncidentOpsReadDbContext).Namespace);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(32);
    }
}
