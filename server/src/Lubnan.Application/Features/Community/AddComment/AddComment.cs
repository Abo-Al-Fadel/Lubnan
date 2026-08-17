using FluentValidation;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Lubnan.Domain.Community;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.Community.AddComment;

public sealed record Command(Guid PostId, string Body) : ICommand<Result<CommentDto>>;

internal sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(c => c.PostId).NotEmpty();
        RuleFor(c => c.Body).NotEmpty().MaximumLength(PostComment.MaxLength);
    }
}

internal sealed class Handler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    : ICommandHandler<Command, Result<CommentDto>>
{
    public async Task<Result<CommentDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } authorId)
        {
            return Result.Failure<CommentDto>(Error.Unauthorized(
                "auth.required", "Sign in to continue."));
        }

        var post = await db.CommunityPosts
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == command.PostId, cancellationToken)
            .ConfigureAwait(false);

        if (post is null)
        {
            return Result.Failure<CommentDto>(Error.NotFound(
                "post.notFound", "That post is gone."));
        }

        var added = post.AddComment(authorId, command.Body, clock.UtcNow);
        if (added.IsFailure)
        {
            return Result.Failure<CommentDto>(added.Error);
        }

        var author = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == authorId)
            .Select(u => u.DisplayName.Value)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (author is null)
        {
            return Result.Failure<CommentDto>(Error.Unauthorized(
                "auth.required", "Sign in to continue."));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var comment = added.Value;
        return Result.Success(new CommentDto(
            comment.Id,
            new AuthorDto(authorId, author),
            comment.Body,
            comment.CreatedAt,
            true));
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/community/posts/{id:guid}/comments", async (
            Guid id,
            CommentBody body,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new Command(id, body.Body), cancellationToken)).ToHttpResult())
        .WithName("AddCommunityComment")
        .WithSummary("Reply to a post. The author is the signed-in account.")
        .WithTags("Community")
        .Produces<CommentDto>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization()
        .RequireRateLimiting(RateLimits.Write);
}

public sealed record CommentBody(string Body);
