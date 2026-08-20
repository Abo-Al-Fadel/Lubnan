using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.Community.RemoveComment;

public sealed record Command(Guid PostId, Guid CommentId) : ICommand<Result>;

internal sealed class Handler(IAppDbContext db, ICurrentUser currentUser)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } requesterId)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        var post = await db.CommunityPosts
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == command.PostId, cancellationToken)
            .ConfigureAwait(false);

        if (post is null)
        {
            return Result.Failure(Error.NotFound("post.notFound", "That post is gone."));
        }

        // The aggregate decides, not this handler. RemoveComment already knows
        // that an author may remove their own and a moderator may remove
        // anyone's, and putting that test here as well would be two copies of
        // one rule - the copy that gets forgotten is always the one in the
        // handler somebody adds next.
        var result = post.RemoveComment(command.CommentId, requesterId, currentUser.IsInRole(Roles.Admin));

        if (result.IsFailure)
        {
            return result;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapDelete("/api/v1/community/posts/{postId:guid}/comments/{commentId:guid}", async (
            Guid postId,
            Guid commentId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new Command(postId, commentId), cancellationToken)).ToHttpResult())
        .WithName("RemoveComment")
        .WithSummary("Delete a comment. Your own, or anyone's if you moderate.")
        .WithTags("Community")
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization()
        .RequireRateLimiting(RateLimits.Write);
}
