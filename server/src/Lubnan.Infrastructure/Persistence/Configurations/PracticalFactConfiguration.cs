using Lubnan.Domain.Places;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lubnan.Infrastructure.Persistence.Configurations;

internal sealed class PracticalFactConfiguration : IEntityTypeConfiguration<PracticalFact>
{
    public void Configure(EntityTypeBuilder<PracticalFact> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("place_facts");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");

        builder.Property(f => f.PlaceId).HasColumnName("place_id");
        builder.Property(f => f.Ordinal).HasColumnName("ordinal");

        builder.HasIndex(f => new { f.PlaceId, f.Ordinal })
            .IsUnique()
            .HasDatabaseName("ix_place_facts_place_ordinal");

        builder.Property<Dictionary<string, FactText>>("_text")
            .HasField("_text")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("text")
            .HasColumnType("jsonb")
            .HasConversion(
                JsonDictionary.Converter<FactText>(),
                JsonDictionary.Comparer<FactText>())
            .IsRequired();

        builder.Ignore(f => f.Text);
    }
}
