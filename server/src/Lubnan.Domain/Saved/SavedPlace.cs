using Lubnan.Domain.Common;
using Lubnan.Domain.Places;

namespace Lubnan.Domain.Saved;

/// <summary>
/// A destination pinned to an account. The slug is the only payload — names
/// and plates are read from the catalogue at query time, so a rename does
/// not leave a stale card on the profile.
/// </summary>
public sealed class SavedPlace : AggregateRoot
{
    private SavedPlace(Guid id, Guid userId, string placeSlug, DateTimeOffset createdAt)
        : base(id)
    {
        UserId = userId;
        PlaceSlug = placeSlug;
        CreatedAt = createdAt;
    }

    private SavedPlace()
    {
    }

    public Guid UserId { get; private init; }

    public string PlaceSlug { get; private init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private init; }

    public static Result<SavedPlace> Pin(Guid userId, string? slug, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure<SavedPlace>(Error.Unauthorized(
                "auth.required", "Sign in to continue."));
        }

        var parsed = Slug.Create(slug);
        if (parsed.IsFailure)
        {
            return Result.Failure<SavedPlace>(parsed.Error);
        }

        return Result.Success(new SavedPlace(Guid.NewGuid(), userId, parsed.Value.Value, now));
    }
}
