using Lubnan.Domain.Places;
using Lubnan.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lubnan.Infrastructure.Persistence.Configurations;

internal sealed class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("places");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Slug)
            .HasConversion(DomainConverters.Slug, DomainConverters.SlugComparer)
            .HasColumnName("slug")
            .HasColumnType("citext")
            .IsRequired();

        // A slug is how a place is linked to from outside. Two rows sharing one
        // is not a data-quality problem to clean up later; it is two pages at
        // one URL, so the database refuses it.
        builder.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("ix_places_slug");

        // Stored by name. An enum persisted as an integer means reordering the
        // members silently rewrites history, and the column is unreadable in
        // psql.
        builder.Property(p => p.Region)
            .HasConversion<string>()
            .HasColumnName("region")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.Category)
            .HasConversion<string>()
            .HasColumnName("category")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.DisplayOrder).HasColumnName("display_order");
        builder.Property(p => p.PublishedAt).HasColumnName("published_at");

        // The public list reads only published rows, so the index covers only
        // published rows. It stays the size of the catalogue rather than the
        // size of the drafts folder, permanently.
        builder.HasIndex(p => new { p.Region, p.DisplayOrder })
            .HasDatabaseName("ix_places_published_region_order")
            .HasFilter("published_at IS NOT NULL");

        // Complex types, not owned entities. An owned reference whose columns
        // are all null materialises as a null navigation, so a place with no
        // plates would arrive with Plates == null and every reader would need a
        // null check for a value object that is never absent. A complex type
        // cannot be null, which is the truth here.
        builder.ComplexProperty(p => p.Coordinates, coordinates =>
        {
            coordinates.Property(c => c.Latitude).HasColumnName("latitude");
            coordinates.Property(c => c.Longitude).HasColumnName("longitude");
        });

        builder.ComplexProperty(p => p.Plates, plates =>
        {
            plates.Property(s => s.Hero).HasColumnName("plate_hero").HasMaxLength(16);
            plates.Property(s => s.Frame).HasColumnName("plate_frame").HasMaxLength(16);
            plates.Property(s => s.Subject).HasColumnName("plate_subject").HasMaxLength(16);
            plates.Property(s => s.Rail).HasColumnName("plate_rail").HasMaxLength(16);
            plates.Property(s => s.Mosaic).HasColumnName("plate_mosaic").HasMaxLength(16);
        });

        // Written by AuditInterceptor into shadow state. Kept off the entity on
        // purpose: "when was this row last touched" is a fact about the row, not
        // about the place, and the domain has no use for it.
        builder.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at");
        builder.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at");

        // Optimistic concurrency on Postgres's own system column. Two editors
        // saving the same place: the second is told the row moved under them,
        // rather than silently overwriting the first. It costs no column of our
        // own and no code in the domain — xmin is already there on every row.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // The children are reached through the aggregate root and deleted with
        // it. Cascade here is not a convenience: an orphan callout is a dot
        // positioned on a photograph that no longer exists.
        builder.HasMany(p => p.Translations)
            .WithOne()
            .HasForeignKey(t => t.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Callouts)
            .WithOne()
            .HasForeignKey(c => c.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PracticalFacts)
            .WithOne()
            .HasForeignKey(f => f.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // The collections are exposed read-only, so EF reads and writes the
        // backing fields rather than going through AsReadOnly().
        builder.Navigation(p => p.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Callouts).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.PracticalFacts).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Raised in memory, drained to the outbox by an interceptor, never a
        // column.
        builder.Ignore(p => p.DomainEvents);
    }
}
