using Lubnan.Domain.Places;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lubnan.Infrastructure.Persistence.Configurations;

internal sealed class CalloutConfiguration : IEntityTypeConfiguration<Callout>
{
    public void Configure(EntityTypeBuilder<Callout> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("place_callouts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.PlaceId).HasColumnName("place_id");
        builder.Property(c => c.Ordinal).HasColumnName("ordinal");
        builder.Property(c => c.X).HasColumnName("x");
        builder.Property(c => c.Y).HasColumnName("y");

        builder.HasIndex(c => new { c.PlaceId, c.Ordinal })
            .IsUnique()
            .HasDatabaseName("ix_place_callouts_place_ordinal");

        // The same bounds the domain checks, enforced by the database as well,
        // because the seeder and any future import write here too.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_place_callouts_within_frame",
            "x >= 0 AND x <= 1 AND y >= 0 AND y <= 1"));

        builder.Property<Dictionary<string, CalloutText>>("_text")
            .HasField("_text")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("text")
            .HasColumnType("jsonb")
            .HasConversion(
                JsonDictionary.Converter<CalloutText>(),
                JsonDictionary.Comparer<CalloutText>())
            .IsRequired();

        builder.Ignore(c => c.Text);
    }
}
