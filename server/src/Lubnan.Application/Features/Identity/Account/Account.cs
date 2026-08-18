using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Application.Abstractions.Security;
using Lubnan.Domain.Common;
using Lubnan.Domain.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lubnan.Application.Features.Identity.Account;

/// <summary>
/// The account-lifecycle endpoints: leaving, changing your mind, and seeing
/// where you are signed in.
/// </summary>
/// <remarks>
/// One file rather than five folders. These are five small operations on one
/// aggregate that are read and changed together — splitting them would mean
/// five folders each holding a twenty-line handler, which is the ceremony that
/// vertical slices exist to avoid, not an example of them.
/// </remarks>
internal static class AccountRoutes
{
    public const string Base = "/api/v1/me";

    /// <summary>
    /// Session routes live under the auth prefix, and they have to.
    /// </summary>
    /// <remarks>
    /// The refresh cookie is deliberately path-scoped to <c>/api/v1/auth</c> so
    /// that ordinary requests do not carry the long-lived credential. Which
    /// means a session list served from <c>/api/v1/me/sessions</c> never
    /// receives it — and "which of these is the device I am using right now"
    /// silently answered "none of them" for every session.
    ///
    /// Moving the route inside the cookie's path is the fix that needs no new
    /// claim in the access token, and therefore no token-format change that
    /// would sign everybody out on deploy.
    /// </remarks>
    public const string Sessions = "/api/v1/auth/sessions";
}

// ── Requesting deletion ─────────────────────────────────────────────────────

public sealed record RequestDeletionCommand(string Password) : ICommand<Result>;

internal sealed class RequestDeletionHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IPasswordHasher passwords,
    IClock clock)
    : ICommandHandler<RequestDeletionCommand, Result>
{
    public async Task<Result> Handle(RequestDeletionCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        var user = await db.Users
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        // Re-authentication, not just a valid session.
        //
        // Deleting an account is the most destructive thing a person can do
        // here, and a live session is exactly what somebody who walked up to an
        // unlocked laptop already has. Asking for the password again is the
        // difference between "this browser is signed in" and "the person who
        // knows the password is here now".
        if (passwords.Verify(user.PasswordHash, command.Password) is PasswordVerification.Failed)
        {
            return Result.Failure(Error.Unauthorized(
                "auth.reauthRequired", "Enter your current password to continue."));
        }

        var result = user.RequestDeletion(clock.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

// ── Cancelling it ───────────────────────────────────────────────────────────

public sealed record CancelDeletionCommand : ICommand<Result>;

internal sealed class CancelDeletionHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    : ICommandHandler<CancelDeletionCommand, Result>
{
    public async Task<Result> Handle(CancelDeletionCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        // No password here, deliberately. Cancelling is the safe direction:
        // the worst case is an account that survives when its owner wanted it
        // gone, and they can ask again. Putting friction on the recovery path
        // of a destructive action gets the asymmetry backwards.
        var result = user.CancelDeletion(clock.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

// ── Sessions ────────────────────────────────────────────────────────────────

/// <param name="Current">
/// Whether this is the session making the request, so the interface can say
/// "this device" and warn before ending it.
/// </param>
public sealed record SessionView(
    Guid Id,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastUsedAt,
    string? UserAgent,
    bool Current);

public sealed record ListSessionsQuery(string? CurrentRefreshToken) : IQuery<Result<IReadOnlyList<SessionView>>>;

internal sealed class ListSessionsHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    ITokenFactory tokens,
    IClock clock)
    : IQueryHandler<ListSessionsQuery, Result<IReadOnlyList<SessionView>>>
{
    public async Task<Result<IReadOnlyList<SessionView>>> Handle(
        ListSessionsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Result.Failure<IReadOnlyList<SessionView>>(
                Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        var now = clock.UtcNow;
        var currentHash = string.IsNullOrEmpty(query.CurrentRefreshToken)
            ? null
            : tokens.HashToken(query.CurrentRefreshToken);

        var sessions = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Sessions)
            .Where(s => s.EndedAt == null && s.ExpiresAt > now)
            .OrderByDescending(s => s.LastUsedAt ?? s.IssuedAt)
            .Select(s => new SessionView(
                s.Id,
                s.IssuedAt,
                s.ExpiresAt,
                s.LastUsedAt,
                s.UserAgent,
                currentHash != null && s.TokenHash == currentHash))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // The token hash never leaves the database, and the IP hash is not
        // projected either. A session list exists so somebody can recognise
        // their own devices; it does not need to hand back the material that
        // would let one be impersonated.
        return Result.Success<IReadOnlyList<SessionView>>(sessions);
    }
}

public sealed record RevokeSessionCommand(Guid SessionId) : ICommand<Result>;

internal sealed class RevokeSessionHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    : ICommandHandler<RevokeSessionCommand, Result>
{
    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        // Loaded through the user, so a session id belonging to somebody else
        // is simply not in the collection. The ownership check is structural
        // rather than an `if` that a later edit could drop.
        var user = await db.Users
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        var result = user.EndSession(command.SessionId, clock.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

// ── Endpoints ───────────────────────────────────────────────────────────────

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost($"{AccountRoutes.Base}/deletion", async (
                RequestDeletionCommand command,
                HttpRequest http,
                IOptions<AuthOptions> options,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);

                if (result.IsFailure)
                {
                    return result.ToHttpResult();
                }

                // The domain already ended every session. Clearing the cookies
                // keeps the browser's view honest rather than leaving it
                // holding credentials the server has stopped accepting.
                AuthCookies.ClearSession(http.HttpContext.Response, options.Value);
                return result.ToHttpResult();
            })
            .WithName("RequestAccountDeletion")
            .WithSummary("Start the 30-day grace period. Requires the current password.")
            .WithTags("Identity")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimits.Auth);

        app.MapDelete($"{AccountRoutes.Base}/deletion", async (
                ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(new CancelDeletionCommand(), cancellationToken)).ToHttpResult())
            .WithName("CancelAccountDeletion")
            .WithSummary("Change your mind, while the grace period is still running.")
            .WithTags("Identity")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimits.Write);

        app.MapGet(AccountRoutes.Sessions, async (
                HttpRequest http, ISender sender, CancellationToken cancellationToken) =>
            {
                var query = new ListSessionsQuery(http.Cookies[AuthCookies.RefreshCookie]);
                return (await sender.Send(query, cancellationToken)).ToHttpResult();
            })
            .WithName("ListSessions")
            .WithSummary("Every device currently signed in.")
            .WithTags("Identity")
            .Produces<IReadOnlyList<SessionView>>()
            .RequireAuthorization()
            .RequireRateLimiting(RateLimits.Read);

        app.MapDelete($"{AccountRoutes.Sessions}/{{id:guid}}", async (
                Guid id, ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(new RevokeSessionCommand(id), cancellationToken)).ToHttpResult())
            .WithName("RevokeSession")
            .WithSummary("End one device's session.")
            .WithTags("Identity")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimits.Write);
    }
}
