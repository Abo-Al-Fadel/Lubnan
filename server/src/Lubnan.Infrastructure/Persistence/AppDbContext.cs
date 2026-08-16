using System.Reflection;
using Lubnan.Application.Abstractions;
using Lubnan.Domain.Places;
using Lubnan.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Infrastructure.Persistence;

/// <summary>The database, as EF sees it.</summary>
/// <remarks>
/// Slices depend on <see cref="IAppDbContext"/>, which exposes the aggregate
/// roots and <c>SaveChangesAsync</c> and nothing else. The outbox set is
/// deliberately not on that interface: it is machinery, and a handler that
/// writes to it directly has bypassed the domain event it should have raised.
/// </remarks>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Place> Places => Set<Place>();

    public DbSet<PlaceTranslation> PlaceTranslations => Set<PlaceTranslation>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // citext, so a slug lookup is case-insensitive in the database rather
        // than by lowering the column in every query, which would defeat the
        // index the moment somebody forgot.
        //
        // PostGIS is deliberately not declared. Nothing queries geography yet,
        // and creating an extension the schema does not use turns "does this
        // managed Postgres offer PostGIS" into a condition of the *first*
        // migration running at all. It arrives with the near-me endpoint, in
        // its own migration, which can then be tested against a real database.
        modelBuilder.HasPostgresExtension("citext");

        // One IEntityTypeConfiguration per aggregate, discovered rather than
        // listed. A configuration that exists is a configuration that applies.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);

        NameConstraintsInSnakeCase(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // timestamptz everywhere. A timestamp without a zone is a number that
        // means different instants in different deployments, and the bug it
        // causes surfaces twice a year.
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("timestamptz");

        // No blanket text mapping for strings. Npgsql already maps an
        // unbounded string to text, and forcing the column type here silently
        // discards every HasMaxLength in the configurations — so a name
        // declared as at most 120 characters would land as unbounded text and
        // the limit would exist only in C#.
        base.ConfigureConventions(configurationBuilder);
    }

    /// <summary>
    /// Renames the constraints EF generates so the whole schema reads in one
    /// case.
    /// </summary>
    /// <remarks>
    /// Columns and indexes are named explicitly in the configurations, because
    /// those names are decisions. Primary and foreign key names are not
    /// decisions — nobody types them — so they are derived here rather than
    /// written out eighty times, and a new table gets the convention for free.
    /// <para>
    /// Mixed case in a Postgres identifier means every hand-written query has
    /// to quote it, and the day somebody forgets, the error message is
    /// "column places.id does not exist" beside a column plainly called Id.
    /// </para>
    /// </remarks>
    private static void NameConstraintsInSnakeCase(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is null)
            {
                continue;
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.IsPrimaryKey()
                    ? $"pk_{table}"
                    : $"uq_{table}_{string.Join('_', key.Properties.Select(p => p.GetColumnName()))}");
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var columns = string.Join('_', foreignKey.Properties.Select(p => p.GetColumnName()));
                foreignKey.SetConstraintName($"fk_{table}_{columns}");
            }
        }
    }
}
