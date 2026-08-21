using System.Reflection;
using Lubnan.Application.Abstractions;
using Lubnan.Domain.Community;
using Lubnan.Domain.Places;
using Lubnan.Domain.Saved;
using Lubnan.Domain.Users;
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

    public DbSet<User> Users => Set<User>();

    public DbSet<Avatar> Avatars => Set<Avatar>();

    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();

    public DbSet<SavedPlace> SavedPlaces => Set<SavedPlace>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public void Untrack(object entity) => Entry(entity).State = EntityState.Detached;

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

        UseApplicationGeneratedKeys(modelBuilder);
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
    /// Tells EF that this application, not the database, decides primary keys.
    /// </summary>
    /// <remarks>
    /// Every entity here assigns <c>Guid.NewGuid()</c> in its constructor,
    /// because an aggregate needs its children's identities before anything is
    /// saved — <c>Callout.PlaceId</c> and <c>UserSession.UserId</c> are set in
    /// memory, long before a transaction exists.
    /// <para>
    /// EF's convention for a <c>Guid</c> key is <c>ValueGeneratedOnAdd</c>, and
    /// the corresponding rule is: <b>if the key already has a value, the row
    /// already exists.</b> So a brand-new session added to a tracked user's
    /// collection was classified as <c>Modified</c>, and EF issued
    /// <c>UPDATE user_sessions … WHERE id = …</c> against a row that had never
    /// been inserted. Zero rows affected, and the exception that surfaces is
    /// <c>DbUpdateConcurrencyException</c> — which points at optimistic
    /// concurrency, somewhere the bug is not.
    /// </para>
    /// <para>
    /// Applied across the model rather than per entity, because the next
    /// aggregate will have the same constructor and would hit the same wall.
    /// </para>
    /// </remarks>
    private static void UseApplicationGeneratedKeys(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var key = entity.FindPrimaryKey();

            if (key is { Properties.Count: 1 } && key.Properties[0].ClrType == typeof(Guid))
            {
                key.Properties[0].ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }
        }
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
