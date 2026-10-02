using IncidentOps.Domain.OnCall;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentOps.Infrastructure.Persistence.Configurations;

internal sealed class EngineerConfiguration : IEntityTypeConfiguration<Engineer>
{
    public void Configure(EntityTypeBuilder<Engineer> builder)
    {
        builder.ToTable("Engineers");
        builder.HasKey(engineer => engineer.Id);
        builder.Property(engineer => engineer.Id).HasMaxLength(64).ValueGeneratedNever();
        builder.Property(engineer => engineer.Name).HasMaxLength(128);
        builder.HasIndex(engineer => new { engineer.Role, engineer.RotationOrder });
    }
}
