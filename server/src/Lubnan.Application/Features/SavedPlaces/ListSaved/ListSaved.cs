using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.SavedPlaces.ListSaved;

public sealed record Query : IQuery<Result<IReadOnlyList<SavedPlaceDto>>>;

internal sealed class Handler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<Query, Result<IReadOnlyList<SavedPlaceDto>>>
{
    public async Task<Result<IReadOnlyList<SavedPlaceDto>>> Handle(
        Query query,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Result.Failure<IReadOnlyList<SavedPlaceDto>>(Error.Unauthorized(
                "auth.required", "Sign in to continue."));
        }

        var rows = await db.SavedPlaces
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SavedPlaceDto(s.PlaceSlug, s.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<IReadOnlyList<SavedPlaceDto>>(rows);
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapGet("/api/v1/me/saved", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new Query(), cancellationToken)).ToHttpResult())
        .WithName("ListSavedPlaces")
        .WithSummary("Destinations pinned to this account.")
        .WithTags("Identity")
        .Produces<IReadOnlyList<SavedPlaceDto>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization()
        .RequireRateLimiting(RateLimits.Read);
}
