using Lubnan.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lubnan.Infrastructure.Persistence.Configurations;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.UserId).HasColumnName("user_id");
        builder.Property(s => s.FamilyId).HasColumnName("family_id");

        // 64 hex characters of SHA-256. Never the token.
        builder.Property(s => s.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();

        // Every refresh is a lookup by this and nothing else, so it is the one
        // index that has to be right. Unique because two rows sharing a hash
        // would make "which session is this" ambiguous at the exact moment it
        // matters.
        builder.HasIndex(s => s.TokenHash).IsUnique().HasDatabaseName("ix_user_sessions_token_hash");

        // Revoking a family is a single indexed delete rather than a scan of
        // every session the user has ever had.
        builder.HasIndex(s => s.FamilyId).HasDatabaseName("ix_user_sessions_family");

        builder.Property(s => s.IssuedAt).HasColumnName("issued_at");
        builder.Property(s => s.ExpiresAt).HasColumnName("expires_at");
        builder.Property(s => s.EndedAt).HasColumnName("ended_at");
        builder.Property(s => s.LastUsedAt).HasColumnName("last_used_at");
        builder.Property(s => s.ReplacedBy).HasColumnName("replaced_by");

        builder.Property(s => s.EndReason)
            .HasConversion<string>()
            .HasColumnName("end_reason")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.UserAgent).HasColumnName("user_agent").HasMaxLength(256);
        builder.Property(s => s.IpHash).HasColumnName("ip_hash").HasMaxLength(32);

        // The session list on the profile page reads only live sessions, and a
        // cleanup job reads only dead ones. Partial, so the index stays the
        // size of "currently signed in" rather than of every sign-in ever.
        builder.HasIndex(s => new { s.UserId, s.ExpiresAt })
            .HasDatabaseName("ix_user_sessions_active")
            .HasFilter("ended_at IS NULL");
    }
}

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.UserId).HasColumnName("user_id");

        builder.Property(t => t.Purpose)
            .HasConversion<string>()
            .HasColumnName("purpose")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();

        // The lookup is by hash *and* purpose, so the index carries both. It
        // also means a confirmation token cannot be found by a reset query even
        // if the hashes somehow collided.
        builder.HasIndex(t => new { t.TokenHash, t.Purpose })
            .IsUnique()
            .HasDatabaseName("ix_user_tokens_hash_purpose");

        builder.Property(t => t.Payload).HasColumnName("payload").HasMaxLength(Email.MaxLength);
        builder.Property(t => t.IssuedAt).HasColumnName("issued_at");
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at");
        builder.Property(t => t.ConsumedAt).HasColumnName("consumed_at");
    }
}
