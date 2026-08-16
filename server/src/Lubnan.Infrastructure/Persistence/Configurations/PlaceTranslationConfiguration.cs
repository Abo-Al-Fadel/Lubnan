using Lubnan.Domain.Places;
using Lubnan.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lubnan.Infrastructure.Persistence.Configurations;

internal sealed class PlaceTranslationConfiguration : IEntityTypeConfiguration<PlaceTranslation>
{
    public void Configure(EntityTypeBuilder<PlaceTranslation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("place_translations");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.PlaceId).HasColumnName("place_id");

        builder.Property(t => t.Locale)
            .HasConversion(DomainConverters.Locale, DomainConverters.LocaleComparer)
            .HasColumnName("locale")
            .HasMaxLength(2)
            .IsRequired();

        // One row per place per language. Two Arabic translations of Byblos is
        // not a state anything downstream knows how to render, so it is not a
        // state the database allows.
        builder.HasIndex(t => new { t.PlaceId, t.Locale })
            .IsUnique()
            .HasDatabaseName("ix_place_translations_place_locale");

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(PlaceTranslation.MaxNameLength)
            .IsRequired();

        builder.Property(t => t.LocalName).HasColumnName("local_name").HasMaxLength(PlaceTranslation.MaxNameLength);
        builder.Property(t => t.Note).HasColumnName("note").IsRequired();
        builder.Property(t => t.Standfirst)
            .HasColumnName("standfirst")
            .HasMaxLength(PlaceTranslation.MaxStandfirstLength)
            .IsRequired();

        builder.Property(t => t.Body).HasColumnName("body").IsRequired();

        // The same rule the domain enforces, enforced again by the database.
        // Not redundancy: the seeder, a migration and psql all write here, and
        // only one of the three goes through the domain.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_place_translations_name_not_blank",
            "length(btrim(name)) > 0"));

        builder.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at");
        builder.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at");
    }
}
