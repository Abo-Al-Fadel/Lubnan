using FluentValidation;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Lubnan.Domain.Places;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.Community.ListPosts;

public sealed record Query(string? Region) : IQuery<Result<IReadOnlyList<PostDto>>>;

internal sealed class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(q => q.Region)
            .Must(value => value is null || RegionNames.TryParse(value, out _))
            .WithMessage($"Unknown region. Expected one of: {string.Join(", ", Enum.GetNames<Region>())}.");
    }
}

internal sealed class Handler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<Query, Result<IReadOnlyList<PostDto>>>
{
    public async Task<Result<IReadOnlyList<PostDto>>> Handle(
        Query query,
        CancellationToken cancellationToken)
    {
        Region? region = query.Region is null || !RegionNames.TryParse(query.Region, out var parsed)
            ? null
            : parsed;

        var viewer = currentUser.Id;

        var rows = await db.CommunityPosts
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(80)
            .Select(p => new
            {
                p.Id,
                p.AuthorId,
                p.Body,
                p.PlaceSlug,
                p.Plate,
                p.CreatedAt,
                LikeCount = p.Likes.Count,
                LikedByMe = viewer != null && p.Likes.Any(l => l.UserId == viewer),
                Comments = p.Comments
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new { c.Id, c.AuthorId, c.Body, c.CreatedAt })
                    .ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var authorIds = rows
            .Select(r => r.AuthorId)
            .Concat(rows.SelectMany(r => r.Comments.Select(c => c.AuthorId)))
            .Distinct()
            .ToList();

        var slugs = rows
            .Select(r => r.PlaceSlug)
            .Where(s => s is not null)
            .Select(s => Slug.Create(s))
            .Where(s => s.IsSuccess)
            .Select(s => s.Value)
            .Distinct()
            .ToList();

        var authors = await db.Users
            .AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.DisplayName.Value })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var names = authors.ToDictionary(a => a.Id, a => a.Name);

        var places = slugs.Count == 0
            ? []
            : await db.Places
                .AsNoTracking()
                .Where(p => slugs.Contains(p.Slug))
                .Select(p => new
                {
                    Slug = p.Slug.Value,
                    p.Region,
                    Name = p.Translations
                        .Where(t => t.Locale == Locale.Default)
                        .Select(t => t.Name)
                        .FirstOrDefault(),
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var placeBySlug = places.ToDictionary(p => p.Slug, StringComparer.OrdinalIgnoreCase);

        var feed = rows
            .Select(row =>
            {
                placeBySlug.TryGetValue(row.PlaceSlug ?? string.Empty, out var place);
                if (region is not null && place?.Region != region)
                {
                    return null;
                }

                return new PostDto(
                    row.Id,
                    new AuthorDto(row.AuthorId, names.GetValueOrDefault(row.AuthorId, "Member")),
                    row.Body,
                    row.PlaceSlug,
                    place?.Name,
                    place?.Region.ToString(),
                    row.Plate,
                    row.CreatedAt,
                    row.LikeCount,
                    row.LikedByMe,
                    row.Comments.Select(c => new CommentDto(
                        c.Id,
                        new AuthorDto(c.AuthorId, names.GetValueOrDefault(c.AuthorId, "Member")),
                        c.Body,
                        c.CreatedAt,
                        viewer is { } id && c.AuthorId == id)).ToList());
            })
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();

        return Result.Success<IReadOnlyList<PostDto>>(feed);
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapGet("/api/v1/community/posts", async (
            string? region,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new Query(region), cancellationToken)).ToHttpResult())
        .WithName("ListCommunityPosts")
        .WithSummary("The community feed.")
        .WithTags("Community")
        .Produces<IReadOnlyList<PostDto>>()
        .RequireRateLimiting(RateLimits.Read);
}
