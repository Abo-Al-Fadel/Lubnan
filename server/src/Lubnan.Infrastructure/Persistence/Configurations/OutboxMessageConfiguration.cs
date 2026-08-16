using Lubnan.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lubnan.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.Type).HasColumnName("type").HasMaxLength(256).IsRequired();
        builder.Property(m => m.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.OccurredAt).HasColumnName("occurred_at");
        builder.Property(m => m.ProcessedAt).HasColumnName("processed_at");
        builder.Property(m => m.Attempts).HasColumnName("attempts");
        builder.Property(m => m.Error).HasColumnName("error");

        // Partial, and this is the detail worth pointing at. The processor asks
        // one question — what is undelivered — so the index answers only that.
        // It stays the size of the backlog rather than the size of all history,
        // which means it stays in memory even after ten million messages.
        builder.HasIndex(m => m.OccurredAt)
            .HasDatabaseName("ix_outbox_pending")
            .HasFilter("processed_at IS NULL");
    }
}
