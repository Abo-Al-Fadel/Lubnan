namespace Lubnan.Application.Features.Community;

public sealed record AuthorDto(Guid Id, string DisplayName);

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
