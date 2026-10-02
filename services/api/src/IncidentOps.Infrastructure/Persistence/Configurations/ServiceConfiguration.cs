using IncidentOps.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentOps.Infrastructure.Persistence.Configurations;

internal sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(service => service.Id);
        builder.Property(service => service.Id)
            .HasConversion(ValueConverters.ServiceId)
            .HasMaxLength(ServiceId.MaxLength)
            .ValueGeneratedNever();
        builder.Property(service => service.Name).HasMaxLength(128);
        builder.Property(service => service.OwnerTeam).HasMaxLength(128);
        builder.Ignore(service => service.DomainEvents);
    }
}
