using System.Globalization;
using FluentValidation;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Lubnan.Domain.Community;
using Lubnan.Domain.Places;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.Community.CreatePost;

public sealed record Command(string Body, string? PlaceSlug, string? Plate)
    : ICommand<Result<PostDto>>;

internal sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(c => c.Body).NotEmpty().MaximumLength(CommunityPost.BodyMaxLength);
        RuleFor(c => c.PlaceSlug).MaximumLength(80);
        RuleFor(c => c.Plate).MaximumLength(CommunityPost.PlateMaxLength);
    }
}

internal sealed class Handler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    : ICommandHandler<Command, Result<PostDto>>
{
    public async Task<Result<PostDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } authorId)
        {
            return Result.Failure<PostDto>(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        var published = CommunityPost.Publish(
            authorId, command.Body, command.PlaceSlug, command.Plate, clock.UtcNow);
        if (published.IsFailure)
        {
            return Result.Failure<PostDto>(published.Error);
        }

        var post = published.Value;

        if (post.PlaceSlug is { } slugValue)
        {
            var slug = Slug.Create(slugValue).Value;
            var exists = await db.Places
                .AsNoTracking()
                .AnyAsync(p => p.Slug == slug && p.PublishedAt != null, cancellationToken)
                .ConfigureAwait(false);

            if (!exists)
            {
                return Result.Failure<PostDto>(Error.NotFound(
                    "place.notFound", "That place is not on the map."));
            }
        }

        // The version, not the picture. This DTO is rendered straight into the
        // feed the moment the post is made, and a new post whose author had no
        // face until the page reloaded would be the one row that looked wrong.
        var author = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == authorId)
            .Select(u => new
            {
                u.Id,
                Name = u.DisplayName.Value,
                AvatarVersion = db.Avatars
                    .Where(a => a.UserId == u.Id)
                    .Select(a => a.UpdatedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (author is null)
        {
            return Result.Failure<PostDto>(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        db.CommunityPosts.Add(post);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        string? placeName = null;
        string? region = null;
        if (post.PlaceSlug is { } publishedSlug)
        {
            var publishedPlace = Slug.Create(publishedSlug).Value;
            var place = await db.Places
                .AsNoTracking()
                .Where(p => p.Slug == publishedPlace)
                .Select(p => new
                {
                    p.Region,
                    Name = p.Translations
                        .Where(t => t.Locale == Locale.Default)
                        .Select(t => t.Name)
                        .FirstOrDefault(),
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            placeName = place?.Name;
            region = place?.Region.ToString();
        }

        return Result.Success(new PostDto(
            post.Id,
            new AuthorDto(author.Id, author.Name, author.AvatarVersion),
            post.Body,
            post.PlaceSlug,
            placeName,
            region,
            post.Plate,
            post.CreatedAt,
            0,
            false,
            []));
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/community/posts", async (
            Command body,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(body, cancellationToken))
                .ToCreatedResult(post => $"/api/v1/community/posts/{post.Id}"))
        .WithName("CreateCommunityPost")
        .WithSummary("Publish a post. The author is the signed-in account, never the body.")
        .WithTags("Community")
        .Produces<PostDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization()
        .RequireRateLimiting(RateLimits.Write);
}
