using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Application.Abstractions.Security;
using Lubnan.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lubnan.Application.Features.Identity.LogoutEverywhere;

/// <summary>
/// End every session on every device.
/// </summary>
/// <remarks>
/// The button somebody presses when they think their password has been seen.
/// It rotates the security stamp as well as ending the sessions, so refresh
/// stops instantly and access tokens stop as they expire.
/// </remarks>
public sealed record Command : ICommand<Result>;

internal sealed class Handler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
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

        user.SignOutEverywhere(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/auth/logout-everywhere", async (
            HttpRequest http,
            IOptions<AuthOptions> options,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new Command(), cancellationToken);
            AuthCookies.ClearSession(http.HttpContext.Response, options.Value);
            return result.ToHttpResult();
        })
        .WithName("LogoutEverywhere")
        .WithSummary("End every session, on every device.")
        .WithTags("Identity")
        .RequireAuthorization()
        .RequireRateLimiting(RateLimits.Auth);
}
