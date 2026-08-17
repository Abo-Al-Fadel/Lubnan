using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.Community.ToggleLike;

public sealed record Command(Guid PostId) : ICommand<Result<LikeStateDto>>;

internal sealed class Handler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    : ICommandHandler<Command, Result<LikeStateDto>>
{
    public async Task<Result<LikeStateDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Result.Failure<LikeStateDto>(Error.Unauthorized(
                "auth.required", "Sign in to continue."));
        }

        var post = await db.CommunityPosts
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.Id == command.PostId, cancellationToken)
            .ConfigureAwait(false);

        if (post is null)
        {
            return Result.Failure<LikeStateDto>(Error.NotFound(
                "post.notFound", "That post is gone."));
        }

        var liked = post.Likes.Any(l => l.UserId == userId);
        var change = liked ? post.Unlike(userId) : post.Like(userId, clock.UtcNow);
        if (change.IsFailure)
        {
            return Result.Failure<LikeStateDto>(change.Error);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var nowLiked = post.Likes.Any(l => l.UserId == userId);
        return Result.Success(new LikeStateDto(nowLiked, post.Likes.Count));
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/community/posts/{id:guid}/like", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new Command(id), cancellationToken)).ToHttpResult())
        .WithName("ToggleCommunityLike")
        .WithSummary("Like or unlike. The person is taken from the session, not the body.")
        .WithTags("Community")
        .Produces<LikeStateDto>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization()
        .RequireRateLimiting(RateLimits.Write);
}
