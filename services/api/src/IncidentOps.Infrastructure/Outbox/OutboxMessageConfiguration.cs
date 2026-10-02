using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentOps.Infrastructure.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Sequence).UseIdentityColumn();
        builder.Property(message => message.Type).HasMaxLength(64);
        builder.Property(message => message.LastError).HasMaxLength(OutboxMessage.ErrorMaxLength);
        builder.HasIndex(message => new { message.ProcessedAt, message.DeadLetteredAt, message.NextAttemptAt, message.Sequence });
    }
}
