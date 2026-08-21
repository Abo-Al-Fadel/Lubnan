using Lubnan.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lubnan.Infrastructure.Persistence.Configurations;

internal sealed class AvatarConfiguration : IEntityTypeConfiguration<Avatar>
{
    public void Configure(EntityTypeBuilder<Avatar> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_avatars");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.UserId).HasColumnName("user_id");

        // One picture per person, enforced by the database rather than by the
        // handler checking first - which is a race whenever somebody
        // double-submits a form.
        builder.HasIndex(a => a.UserId).IsUnique().HasDatabaseName("ix_user_avatars_user");

        builder.Property(a => a.Content).HasColumnName("content").HasColumnType("bytea").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        // Cascade, unlike account_events. An avatar is the person's own data
        // with no evidentiary value, so it should go when they do - the audit
        // trail is the thing that has to outlive them, not their photograph.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
