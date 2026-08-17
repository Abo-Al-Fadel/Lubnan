using Lubnan.Domain.Community;
using Lubnan.Domain.Places;
using Lubnan.Domain.Saved;
using Lubnan.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Abstractions;

/// <summary>
/// What a slice is allowed to see of the database.
/// </summary>
/// <remarks>
/// Deliberately the aggregate roots and <c>SaveChangesAsync</c>, and nothing
/// else. No <c>Database</c> property, so a handler cannot open its own
/// transaction behind the pipeline's back or run raw SQL that the architecture
/// tests cannot see.
/// <para>
/// <see cref="UserSession"/>, <see cref="UserToken"/> and
/// <see cref="AccountEvent"/> are absent on purpose. They live inside the
/// <see cref="User"/> aggregate and are reached through it, which is what keeps
/// "ending a session also bumps the security stamp" true everywhere rather than
/// in whichever handlers remembered.
/// </para>
/// </remarks>
public interface IAppDbContext
{
    DbSet<Place> Places { get; }

    DbSet<PlaceTranslation> PlaceTranslations { get; }

    DbSet<User> Users { get; }

    DbSet<CommunityPost> CommunityPosts { get; }

    DbSet<SavedPlace> SavedPlaces { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
