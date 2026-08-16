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

namespace Lubnan.Application.Features.Identity.Refresh;

public sealed record Command(string RefreshToken, RequestFingerprint Fingerprint)
    : ICommand<Result<SessionGrant>>;

internal sealed class Handler(
    IAppDbContext db,
    ITokenFactory tokens,
    IIpHasher ipHasher,
    IOptions<AuthOptions> options,
    IClock clock)
    : ICommandHandler<Command, Result<SessionGrant>>
{
    private static readonly Error Denied = Error.Unauthorized(
        "auth.sessionEnded", "That session has ended. Sign in again.");

    public async Task<Result<SessionGrant>> Handle(Command command, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var hash = tokens.HashToken(command.RefreshToken);

        // Found by hash, across all users, then the user is loaded with the
        // whole family. Loading the family matters: revoking on reuse has to
        // reach the thief's current token, not just the one presented.
        var user = await db.Users
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Sessions.Any(s => s.TokenHash == hash), cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            // No such token. Either it never existed or the family was already
            // revoked and swept. Nothing to revoke, nothing to say.
            return Result.Failure<SessionGrant>(Denied);
        }

        var session = user.Sessions.First(s => s.TokenHash == hash);

        // The token exists but has already been rotated, signed out or revoked.
        // This is the interesting case: a live token would never be presented
        // twice by a well-behaved client, so somebody has a copy.
        if (!session.IsActive)
        {
            var reuse = user.DetectReuse(session, now);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure<SessionGrant>(reuse.Error);
        }

        if (session.IsExpired(now) || !user.CanSignIn(now))
        {
            // Through the aggregate, not by calling End on the session. The
            // session's mutators are internal to the domain assembly precisely
            // so that every change goes past the root, where the rules are.
            user.EndSession(session.Id, now);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure<SessionGrant>(Denied);
        }

        var grant = SessionIssuer.Issue(
            user,
            tokens,
            options.Value,
            now,
            command.Fingerprint.UserAgent,
            ipHasher.Hash(command.Fingerprint.Ip),
            rotating: session);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(grant);
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/auth/refresh", async (
            HttpRequest http,
            IOptions<AuthOptions> options,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var token = http.Cookies[AuthCookies.RefreshCookie];

            if (string.IsNullOrEmpty(token))
            {
                return Results.Problem(
                    title: "That session has ended. Sign in again.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    type: "https://lubnan.app/errors/unauthorized",
                    extensions: new Dictionary<string, object?> { ["code"] = "auth.sessionEnded" });
            }

            var result = await sender.Send(new Command(token, http.Fingerprint()), cancellationToken);

            if (result.IsFailure)
            {
                // Clear the cookies on any failure. Leaving a dead refresh
                // token in the browser means the client retries with it
                // forever, and every retry looks like reuse.
                AuthCookies.ClearSession(http.HttpContext.Response, options.Value);
                return result.ToHttpResult();
            }

            result.Value.Write(http.HttpContext.Response, options.Value);
            return Results.NoContent();
        })
        .WithName("Refresh")
        .WithSummary("Rotate the session. Reusing a spent token revokes the whole family.")
        .WithTags("Identity")
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireRateLimiting(RateLimits.Auth)
        .AllowAnonymous();
}
