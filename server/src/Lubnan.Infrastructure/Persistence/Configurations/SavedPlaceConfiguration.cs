using Lubnan.Domain.Saved;
using Lubnan.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lubnan.Infrastructure.Persistence.Configurations;

internal sealed class SavedPlaceConfiguration : IEntityTypeConfiguration<SavedPlace>
{
    public void Configure(EntityTypeBuilder<SavedPlace> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("saved_places");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.PlaceSlug)
            .HasColumnName("place_slug")
            .HasColumnType("citext")
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(s => new { s.UserId, s.PlaceSlug })
            .IsUnique()
            .HasDatabaseName("ix_saved_places_user_slug");

        builder.HasIndex(s => s.UserId).HasDatabaseName("ix_saved_places_user");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.DomainEvents);
    }
}
