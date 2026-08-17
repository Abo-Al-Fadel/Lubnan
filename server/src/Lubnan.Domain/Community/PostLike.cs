using Lubnan.Domain.Common;

namespace Lubnan.Domain.Community;

/// <summary>One person liking one post. The pair is unique.</summary>
public sealed class PostLike : Entity
{
    internal PostLike(Guid id, Guid postId, Guid userId, DateTimeOffset createdAt)
        : base(id)
    {
        PostId = postId;
        UserId = userId;
        CreatedAt = createdAt;
    }

    private PostLike() { }

    public Guid PostId { get; private init; }

    public Guid UserId { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }
}
