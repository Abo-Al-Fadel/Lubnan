using Lubnan.Domain.Common;
using Lubnan.Domain.Places;

namespace Lubnan.Domain.Community;

/// <summary>
/// A feed post. Likes and comments live inside this boundary so "one like
/// per person" and "a comment belongs to a real post" are checked here,
/// not in whichever handler remembered.
/// </summary>
public sealed class CommunityPost : AggregateRoot
{
    public const int BodyMinLength = 1;
    public const int BodyMaxLength = 2000;
    public const int PlateMaxLength = 16;

    private readonly List<PostLike> _likes = [];
    private readonly List<PostComment> _comments = [];

    private CommunityPost(
        Guid id,
        Guid authorId,
        string body,
        string? placeSlug,
        string? plate,
        DateTimeOffset createdAt)
        : base(id)
    {
        AuthorId = authorId;
        Body = body;
        PlaceSlug = placeSlug;
        Plate = plate;
        CreatedAt = createdAt;
    }

    private CommunityPost() { }

    public Guid AuthorId { get; private init; }

    public string Body { get; private init; } = string.Empty;

    public string? PlaceSlug { get; private init; }

    public string? Plate { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public IReadOnlyList<PostLike> Likes => _likes.AsReadOnly();

    public IReadOnlyList<PostComment> Comments => _comments.AsReadOnly();

    public static Result<CommunityPost> Publish(
        Guid authorId,
        string? body,
        string? placeSlug,
        string? plate,
        DateTimeOffset now)
    {
        if (authorId == Guid.Empty)
        {
            return Result.Failure<CommunityPost>(Error.Unauthorized(
                "auth.required", "Sign in to continue."));
        }

        var text = Normalise(body);
        if (text.Length is < BodyMinLength or > BodyMaxLength)
        {
            return Result.Failure<CommunityPost>(Error.Validation(
                "post.body.length",
                $"A post is between {BodyMinLength} and {BodyMaxLength} characters."));
        }

        if (TextRules.HasForbiddenMarks(text))
        {
            return Result.Failure<CommunityPost>(Error.Validation(
                "post.body.characters",
                "A post cannot contain formatting or control characters."));
        }

        string? slug = null;
        if (!string.IsNullOrWhiteSpace(placeSlug))
        {
            var parsed = Slug.Create(placeSlug);
            if (parsed.IsFailure)
            {
                return Result.Failure<CommunityPost>(parsed.Error);
            }

            slug = parsed.Value.Value;
        }

        var plateId = NormalisePlate(plate);
        if (plate is not null && plateId is null && plate.Trim().Length > 0)
        {
            return Result.Failure<CommunityPost>(Error.Validation(
                "post.plate.malformed", "That plate id is not usable."));
        }

        return Result.Success(new CommunityPost(Guid.NewGuid(), authorId, text, slug, plateId, now));
    }

    public Result Like(Guid userId, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        if (_likes.Any(like => like.UserId == userId))
        {
            return Result.Success();
        }

        _likes.Add(new PostLike(Guid.NewGuid(), Id, userId, now));
        return Result.Success();
    }

    public Result Unlike(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        _likes.RemoveAll(like => like.UserId == userId);
        return Result.Success();
    }

    public Result<PostComment> AddComment(Guid authorId, string? body, DateTimeOffset now)
    {
        if (authorId == Guid.Empty)
        {
            return Result.Failure<PostComment>(Error.Unauthorized(
                "auth.required", "Sign in to continue."));
        }

        var text = Normalise(body);
        if (text.Length is < PostComment.MinLength or > PostComment.MaxLength)
        {
            return Result.Failure<PostComment>(Error.Validation(
                "comment.body.length",
                $"A comment is between {PostComment.MinLength} and {PostComment.MaxLength} characters."));
        }

        if (TextRules.HasForbiddenMarks(text))
        {
            return Result.Failure<PostComment>(Error.Validation(
                "comment.body.characters",
                "A comment cannot contain formatting or control characters."));
        }

        var comment = new PostComment(Guid.NewGuid(), Id, authorId, text, now);
        _comments.Add(comment);
        return Result.Success(comment);
    }

    public Result RemoveComment(Guid commentId, Guid requesterId, bool isAdmin)
    {
        var comment = _comments.FirstOrDefault(c => c.Id == commentId);
        if (comment is null)
        {
            return Result.Failure(Error.NotFound("comment.notFound", "That comment is gone."));
        }

        if (!isAdmin && comment.AuthorId != requesterId)
        {
            return Result.Failure(Error.Forbidden(
                "comment.forbidden", "You can only remove your own comment."));
        }

        _comments.Remove(comment);
        return Result.Success();
    }

    private static string Normalise(string? value) =>
        string.Join(' ', (value ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string? NormalisePlate(string? value)
    {
        var candidate = value?.Trim();
        if (string.IsNullOrEmpty(candidate))
        {
            return null;
        }

        if (candidate.Length > PlateMaxLength)
        {
            return null;
        }

        return candidate.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? candidate
            : null;
    }
}
