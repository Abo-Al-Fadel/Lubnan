using Lubnan.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lubnan.Infrastructure.Persistence.Configurations;

/// <summary>
/// The audit trail, mapped so the application can only ever add to it.
/// </summary>
/// <remarks>
/// Every property is <c>ValueGeneratedNever</c> and mapped
/// <see cref="PropertyAccessMode.Field"/> with no setter reachable from
/// outside the aggregate, and <c>AuditInterceptor</c> is instructed to leave
/// these rows alone. The application-level guarantee is that no code path
/// exists which updates or deletes a row here.
/// <para>
/// The database-level guarantee is stronger and belongs beside it: a role that
/// holds INSERT and SELECT on this table and nothing else. That is a grant in a
/// migration, not a mapping, and it is what makes the log survive an attacker
/// who has the application's own credentials. It is written down in the README
/// as the next hardening step rather than pretended to here.
/// </para>
/// </remarks>
internal sealed class AccountEventConfiguration : IEntityTypeConfiguration<AccountEvent>
{
    public void Configure(EntityTypeBuilder<AccountEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("account_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.UserId).HasColumnName("user_id").ValueGeneratedNever();

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasColumnName("type")
            .HasMaxLength(40)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(e => e.ActorId).HasColumnName("actor_id").ValueGeneratedNever();
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(500).ValueGeneratedNever();
        builder.Property(e => e.IpHash).HasColumnName("ip_hash").HasMaxLength(32).ValueGeneratedNever();
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").ValueGeneratedNever();

        // "What happened to this account, most recent first" is the query an
        // operator runs during an incident and the one behind the security page
        // on a profile. Descending, because nobody reads it forwards.
        builder.HasIndex(e => new { e.UserId, e.OccurredAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_account_events_user_time");

        // "Everything this moderator has done", for reviewing a suspension or
        // for noticing that an admin account has started behaving oddly.
        builder.HasIndex(e => new { e.ActorId, e.OccurredAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_account_events_actor_time")
            .HasFilter("actor_id IS NOT NULL");
    }
}
