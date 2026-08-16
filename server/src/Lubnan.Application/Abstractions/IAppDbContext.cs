using Lubnan.Domain.Places;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Abstractions;

/// <summary>
/// What a slice is allowed to see of the database.
/// </summary>
/// <remarks>
/// Deliberately the DbSets and <c>SaveChangesAsync</c>, and nothing else. No
/// <c>Database</c> property, so a handler cannot open its own transaction
/// behind the pipeline's back or run raw SQL that the architecture tests
/// cannot see.
/// </remarks>
public interface IAppDbContext
{
    DbSet<Place> Places { get; }

    DbSet<PlaceTranslation> PlaceTranslations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
