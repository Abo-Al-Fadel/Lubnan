using FluentValidation;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Lubnan.Domain.Places;
using Lubnan.Domain.Saved;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.SavedPlaces.PinSaved;

public sealed record Command(string Slug) : ICommand<Result<SavedPlaceDto>>;

internal sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(Slug.MaxLength);
    }
}

internal sealed class Handler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    : ICommandHandler<Command, Result<SavedPlaceDto>>
{
    public async Task<Result<SavedPlaceDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Result.Failure<SavedPlaceDto>(Error.Unauthorized(
                "auth.required", "Sign in to continue."));
        }

        var pinned = SavedPlace.Pin(userId, command.Slug, clock.UtcNow);
        if (pinned.IsFailure)
        {
            return Result.Failure<SavedPlaceDto>(pinned.Error);
        }

        var saved = pinned.Value;
        var slug = Slug.Create(saved.PlaceSlug).Value;

        var exists = await db.Places
            .AsNoTracking()
            .AnyAsync(p => p.Slug == slug && p.PublishedAt != null, cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            return Result.Failure<SavedPlaceDto>(Error.NotFound(
                "place.notFound", "That place is not on the map."));
        }

        var already = await db.SavedPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.UserId == userId && s.PlaceSlug == saved.PlaceSlug,
                cancellationToken)
            .ConfigureAwait(false);

        if (already is not null)
        {
            return Result.Success(new SavedPlaceDto(already.PlaceSlug, already.CreatedAt));
        }

        db.SavedPlaces.Add(saved);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            var raced = await db.SavedPlaces
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.UserId == userId && s.PlaceSlug == saved.PlaceSlug,
                    cancellationToken)
                .ConfigureAwait(false);

            if (raced is null)
            {
                throw;
            }

            return Result.Success(new SavedPlaceDto(raced.PlaceSlug, raced.CreatedAt));
        }

        return Result.Success(new SavedPlaceDto(saved.PlaceSlug, saved.CreatedAt));
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/me/saved", async (
            Command body,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(body, cancellationToken))
                .ToCreatedResult(row => $"/api/v1/me/saved/{row.Slug}"))
        .WithName("PinSavedPlace")
        .WithSummary("Pin a destination to this account.")
        .WithTags("Identity")
        .Produces<SavedPlaceDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization()
        .RequireRateLimiting(RateLimits.Write);
}
