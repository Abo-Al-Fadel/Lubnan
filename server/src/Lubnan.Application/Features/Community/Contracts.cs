namespace Lubnan.Application.Features.Community;

/// <param name="AvatarVersion">
/// When the picture last changed, as a unix timestamp, or <c>null</c> for
/// somebody who has not set one.
/// <para>
/// It travels with the author rather than being fetched per face, and it earns
/// its place twice. It answers "is there a picture" without a request that
/// 404s for most members, and it is the cache key: the avatar route is served
/// <c>immutable</c> for a year, so a URL without this would keep showing the
/// old face long after it was replaced.
/// </para>
/// </param>
public sealed record AuthorDto(Guid Id, string DisplayName, string? AvatarVersion);

public sealed record CommentDto(
    Guid Id,
    AuthorDto Author,
    string Body,
    DateTimeOffset CreatedAt,
    bool Mine);

public sealed record PostDto(
    Guid Id,
    AuthorDto Author,
    string Body,
    string? PlaceSlug,
    string? PlaceName,
    string? Region,
    string? Plate,
    DateTimeOffset CreatedAt,
    int LikeCount,
    bool LikedByMe,
    IReadOnlyList<CommentDto> Comments);

public sealed record LikeStateDto(bool Liked, int LikeCount);
