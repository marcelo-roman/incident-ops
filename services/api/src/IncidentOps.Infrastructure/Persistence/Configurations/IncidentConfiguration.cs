using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentOps.Infrastructure.Persistence.Configurations;

internal sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public const string RowVersion = "RowVersion";
    public const string OpenAlertFingerprintIndex = "IX_Incidents_OpenAlertFingerprint";

    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incidents");
        builder.HasKey(incident => incident.Id);
        builder.Property(incident => incident.Id).HasConversion(ValueConverters.IncidentId).ValueGeneratedNever();
        builder.Property(incident => incident.Number).HasConversion(ValueConverters.IncidentNumber);
        builder.Property(incident => incident.Title).HasConversion(ValueConverters.Title).HasMaxLength(IncidentTitle.MaxLength);
        builder.Property(incident => incident.Description).HasConversion(ValueConverters.Description).HasMaxLength(Description.MaxLength);
        builder.Property(incident => incident.ServiceId).HasConversion(ValueConverters.ServiceId).HasMaxLength(ServiceId.MaxLength);
        builder.Property(incident => incident.Assignee).HasConversion(ValueConverters.OptionalActor).HasMaxLength(Actor.MaxLength);
        builder.Property(incident => incident.EscalationLevel).HasConversion(ValueConverters.EscalationLevel);
        builder.Property(incident => incident.RootCause).HasConversion(ValueConverters.OptionalRootCause).HasMaxLength(RootCause.MaxLength);
        builder.Property(incident => incident.AlertFingerprint).HasConversion(ValueConverters.OptionalFingerprint).HasMaxLength(AlertFingerprint.MaxLength);
        builder.OwnsOne(incident => incident.Sla, sla =>
        {
            sla.WithOwner();
            sla.Property(clock => clock.StartedAt).HasColumnName("CreatedAt");
            sla.Property(clock => clock.AckWindowStartsAt).HasColumnName("AckWindowStartsAt");
            sla.Property(clock => clock.AckDueAt).HasColumnName("AckDueAt");
            sla.Property(clock => clock.ResolveDueAt).HasColumnName("ResolveDueAt");
            sla.Property(clock => clock.AcknowledgementBreached).HasColumnName("AcknowledgementBreached");
            sla.HasIndex(clock => clock.StartedAt).HasDatabaseName("IX_Incidents_CreatedAt");
        });
        builder.Navigation(incident => incident.Sla).IsRequired();
        builder.Property<byte[]>(RowVersion).IsRowVersion();
        builder.Ignore(incident => incident.CreatedAt);
        builder.Ignore(incident => incident.IsOpen);
        builder.Ignore(incident => incident.DomainEvents);

        builder.HasOne<Service>().WithMany().HasForeignKey(incident => incident.ServiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(incident => incident.Timeline)
            .WithOne()
            .HasForeignKey(entry => entry.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(incident => incident.Timeline).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(incident => incident.Number).IsUnique();
        builder.HasIndex(incident => incident.Status);
        builder.HasIndex(incident => incident.AlertFingerprint)
            .IsUnique()
            .HasDatabaseName(OpenAlertFingerprintIndex)
            .HasFilter("[AlertFingerprint] IS NOT NULL AND [Status] <> 'Resolved'");
    }
}
