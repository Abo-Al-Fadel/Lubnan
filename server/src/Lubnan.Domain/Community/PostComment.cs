using Lubnan.Domain.Common;

namespace Lubnan.Domain.Community;

/// <summary>A reply on a post. Reached only through <see cref="CommunityPost"/>.</summary>
public sealed class PostComment : Entity
{
    public const int MinLength = 1;
    public const int MaxLength = 500;

    internal PostComment(Guid id, Guid postId, Guid authorId, string body, DateTimeOffset createdAt)
        : base(id)
    {
        PostId = postId;
        AuthorId = authorId;
        Body = body;
        CreatedAt = createdAt;
    }

    private PostComment() { }

    public Guid PostId { get; private init; }

    public Guid AuthorId { get; private init; }

    public string Body { get; private init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private init; }
}
