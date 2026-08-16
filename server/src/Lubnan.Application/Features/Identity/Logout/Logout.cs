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

namespace Lubnan.Application.Features.Identity.Logout;

/// <summary>End this device's session. The others keep working.</summary>
public sealed record Command(string? RefreshToken) : ICommand<Result>;

internal sealed class Handler(IAppDbContext db, ITokenFactory tokens, IClock clock)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        // Always succeeds, whatever it is given.
        //
        // Signing out is the one action that must never fail. A 401 from this
        // endpoint leaves someone looking at a page that says they are signed
        // in, on a device they are trying to leave - and there is nothing
        // useful they could do about it. An unknown token means the session is
        // already gone, which is the outcome they asked for.
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return Result.Success();
        }

        var hash = tokens.HashToken(command.RefreshToken);

        var user = await db.Users
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Sessions.Any(s => s.TokenHash == hash), cancellationToken)
            .ConfigureAwait(false);

        var session = user?.Sessions.FirstOrDefault(s => s.TokenHash == hash);

        if (user is not null && session is not null)
        {
            user.EndSession(session.Id, clock.UtcNow);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/auth/logout", async (
            HttpRequest http,
            IOptions<AuthOptions> options,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new Command(http.Cookies[AuthCookies.RefreshCookie]), cancellationToken);

            // Cookies go regardless of what the server found. The browser's
            // state is what the person is looking at.
            AuthCookies.ClearSession(http.HttpContext.Response, options.Value);

            return result.ToHttpResult();
        })
        .WithName("Logout")
        .WithSummary("End this session. Always succeeds.")
        .WithTags("Identity")
        .RequireRateLimiting(RateLimits.Auth)
        .AllowAnonymous();
}
