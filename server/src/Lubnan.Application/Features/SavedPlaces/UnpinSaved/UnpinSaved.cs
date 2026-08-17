using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Lubnan.Domain.Places;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.SavedPlaces.UnpinSaved;

public sealed record Command(string Slug) : ICommand<Result>;

internal sealed class Handler(IAppDbContext db, ICurrentUser currentUser)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        var parsed = Slug.Create(command.Slug);
        if (parsed.IsFailure)
        {
            return Result.Success();
        }

        var row = await db.SavedPlaces
            .FirstOrDefaultAsync(
                s => s.UserId == userId && s.PlaceSlug == parsed.Value.Value,
                cancellationToken)
            .ConfigureAwait(false);

        if (row is not null)
        {
            db.SavedPlaces.Remove(row);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapDelete("/api/v1/me/saved/{slug}", async (
            string slug,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new Command(slug), cancellationToken)).ToHttpResult())
        .WithName("UnpinSavedPlace")
        .WithSummary("Remove a destination from this account.")
        .WithTags("Identity")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization()
        .RequireRateLimiting(RateLimits.Write);
}
