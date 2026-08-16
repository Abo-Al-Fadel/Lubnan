using Lubnan.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lubnan.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Email)
            .HasConversion(UserConverters.Email, UserConverters.EmailComparer)
            .HasColumnName("email")
            .HasColumnType("citext")
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        // One account per address, enforced by the database. A handler that
        // checks first and inserts second has a race between the two, and the
        // race is won by whoever registers twice in the same second - which is
        // exactly what an automated signup does.
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_users_email");

        builder.Property(u => u.DisplayName)
            .HasConversion(UserConverters.DisplayName, UserConverters.DisplayNameComparer)
            .HasColumnName("display_name")
            .HasMaxLength(DisplayName.MaxLength)
            .IsRequired();

        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(256).IsRequired();
        builder.Property(u => u.EmailConfirmed).HasColumnName("email_confirmed");
        builder.Property(u => u.IsAdmin).HasColumnName("is_admin");
        builder.Property(u => u.SecurityStamp).HasColumnName("security_stamp");

        builder.Property(u => u.State)
            .HasConversion<string>()
            .HasColumnName("state")
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.LastSignedInAt).HasColumnName("last_signed_in_at");
        builder.Property(u => u.FailedSignInCount).HasColumnName("failed_sign_in_count");
        builder.Property(u => u.LockedUntil).HasColumnName("locked_until");

        builder.Property(u => u.SuspendedAt).HasColumnName("suspended_at");
        builder.Property(u => u.SuspendedUntil).HasColumnName("suspended_until");
        builder.Property(u => u.SuspensionReason).HasColumnName("suspension_reason").HasMaxLength(500);

        builder.Property(u => u.DeletionRequestedAt).HasColumnName("deletion_requested_at");
        builder.Property(u => u.PurgeAfter).HasColumnName("purge_after");
        builder.Property(u => u.AnonymisedAt).HasColumnName("anonymised_at");

        // The purge worker asks one question: which accounts are past their
        // grace period. A partial index answers exactly that and stays the size
        // of the queue rather than the size of the user table.
        builder.HasIndex(u => u.PurgeAfter)
            .HasDatabaseName("ix_users_pending_purge")
            .HasFilter("purge_after IS NOT NULL AND anonymised_at IS NULL");

        builder.HasMany(u => u.Sessions)
            .WithOne()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Tokens)
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade, and this one is not a style choice. The audit
        // trail must outlive the account it describes: if deleting a user took
        // the record of why they were suspended with it, the log would be
        // erasable by the very action it exists to document. Nothing deletes a
        // user row anyway - Anonymise overwrites it - so this is a second lock
        // on a door that should already be shut.
        builder.HasMany(u => u.AccountEvents)
            .WithOne()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(u => u.Sessions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(u => u.Tokens).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(u => u.AccountEvents).UsePropertyAccessMode(PropertyAccessMode.Field);

        // No concurrency token on the user row, and this is deliberate rather
        // than an oversight.
        //
        // A place is edited by one person occasionally, so telling the second
        // editor their copy is stale is exactly right. A user row is written on
        // every sign-in, every refresh and every failed attempt — so two tabs
        // refreshing at the same moment is *normal*, and a token here would
        // turn that into a 500 on the most important endpoint in the system.
        // The invariants that actually matter are enforced by unique indexes on
        // the email and the session token hash, which do not care about
        // ordering.

        builder.Ignore(u => u.DomainEvents);
    }
}
