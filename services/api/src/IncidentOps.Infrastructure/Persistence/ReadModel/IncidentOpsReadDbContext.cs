using IncidentOps.Application.Incidents.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace IncidentOps.Infrastructure.Persistence.ReadModel;

public sealed class IncidentOpsReadDbContext(DbContextOptions<IncidentOpsReadDbContext> options) : DbContext(options)
{
    internal DbSet<IncidentRecord> Incidents => Set<IncidentRecord>();

    internal DbSet<TimelineEntryRecord> TimelineEntries => Set<TimelineEntryRecord>();

    internal DbSet<ServiceRow> Services => Set<ServiceRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IncidentRecord>(incident =>
        {
            incident.ToTable("Incidents", table => table.ExcludeFromMigrations());
            incident.HasKey(record => record.Id);
            incident.Ignore(record => record.IsOpen);
        });

        modelBuilder.Entity<TimelineEntryRecord>(entry =>
        {
            entry.ToTable("TimelineEntries", table => table.ExcludeFromMigrations());
            entry.HasKey(record => record.Id);
        });

        modelBuilder.Entity<ServiceRow>(service =>
        {
            service.ToTable("Services", table => table.ExcludeFromMigrations());
            service.HasKey(row => row.Id);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }
}
