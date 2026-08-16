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

namespace Lubnan.Application.Features.Identity.ConfirmEmail;

public sealed record Command(string Token) : ICommand<Result>;

internal sealed class Handler(IAppDbContext db, ITokenFactory tokens, IClock clock)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var hash = tokens.HashToken(command.Token);

        var user = await db.Users
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(
                u => u.Tokens.Any(t => t.TokenHash == hash && t.Purpose == TokenPurpose.ConfirmEmail),
                cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(Error.Validation(
                "token.invalid", "That link is no longer valid. Request a new one."));
        }

        var consumed = user.ConsumeToken(TokenPurpose.ConfirmEmail, hash, now);
        if (consumed.IsFailure)
        {
            return consumed;
        }

        user.ConfirmEmail(now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // No session is issued here. A confirmation link travels through mail
        // servers, spam filters, link scanners and whatever chat it was
        // forwarded to; making it sign the clicker in would mean anything that
        // fetched the URL became a logged-in browser. Confirm, then sign in.
        return Result.Success();
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/auth/confirm-email", async (
            Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(command, cancellationToken)).ToHttpResult())
        .WithName("ConfirmEmail")
        .WithSummary("Prove an address exists. Does not sign anybody in.")
        .WithTags("Identity")
        .ProducesValidationProblem()
        .RequireRateLimiting(RateLimits.Auth)
        .AllowAnonymous();
}
