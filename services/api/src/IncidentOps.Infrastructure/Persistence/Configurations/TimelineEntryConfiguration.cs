using IncidentOps.Domain.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentOps.Infrastructure.Persistence.Configurations;

internal sealed class TimelineEntryConfiguration : IEntityTypeConfiguration<TimelineEntry>
{
    public void Configure(EntityTypeBuilder<TimelineEntry> builder)
    {
        builder.ToTable("TimelineEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();
        builder.Property(entry => entry.IncidentId).HasConversion(ValueConverters.IncidentId);
        builder.Property(entry => entry.Actor).HasConversion(ValueConverters.Actor).HasMaxLength(Actor.MaxLength);
        builder.Property(entry => entry.Message).HasMaxLength(TimelineEntry.MessageMaxLength);
        builder.HasIndex(entry => new { entry.IncidentId, entry.Sequence }).IsUnique();
    }
}
