using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.Identity.GetMe;

public sealed record Query : IQuery<Result<Me>>;

/// <summary>
/// What the profile page needs. Nothing more.
/// </summary>
/// <param name="PendingDeletionUntil">
/// Set while the account is in its grace period, so the frontend can show the
/// banner that offers to cancel. It is the only way somebody who did not
/// request the deletion finds out in the app rather than by email.
/// </param>
public sealed record Me(
    Guid Id,
    string Email,
    string DisplayName,
    bool EmailConfirmed,
    bool IsAdmin,
    string State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PendingDeletionUntil,
    int ActiveSessions);

internal sealed class Handler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    : IQueryHandler<Query, Result<Me>>
{
    public async Task<Result<Me>> Handle(Query query, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Result.Failure<Me>(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        var now = clock.UtcNow;

        var me = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new Me(
                u.Id,
                u.Email.Value,
                u.DisplayName.Value,
                u.EmailConfirmed,
                u.IsAdmin,
                u.State.ToString(),
                u.CreatedAt,
                u.PurgeAfter,
                u.Sessions.Count(s => s.EndedAt == null && s.ExpiresAt > now)))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // A valid token for a user who is not there. The row was anonymised, or
        // the database was restored from before they registered. Either way the
        // token outlived its subject and the answer is to sign in again.
        return me is null
            ? Result.Failure<Me>(Error.Unauthorized("auth.required", "Sign in to continue."))
            : Result.Success(me);
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapGet("/api/v1/me", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new Query(), cancellationToken)).ToHttpResult())
        .WithName("GetMe")
        .WithSummary("The signed-in account.")
        .WithTags("Identity")
        .Produces<Me>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization()
        .RequireRateLimiting(RateLimits.Read);
}
