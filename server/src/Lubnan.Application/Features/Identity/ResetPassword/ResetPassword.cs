using FluentValidation;
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

namespace Lubnan.Application.Features.Identity.ResetPassword;

public sealed record Command(string Token, string Password) : ICommand<Result>;

internal sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(c => c.Token).NotEmpty().MaximumLength(200);

        RuleFor(c => c.Password)
            .MinimumLength(12).WithMessage("Use at least 12 characters. A short sentence works well.")
            .MaximumLength(256).WithMessage("That password is too long.");
    }
}

internal sealed class Handler(
    IAppDbContext db,
    ITokenFactory tokens,
    IPasswordHasher passwords,
    IBreachedPasswordCheck breached,
    IClock clock)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        // Before the token is spent, not after. Rejecting the new password
        // once the token was already consumed would leave somebody locked out
        // holding a link that no longer works, mid-reset.
        if (await breached.IsBreachedAsync(command.Password, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.Validation(
                "password.breached",
                "That password has appeared in a public data breach. Choose a different one."));
        }

        var now = clock.UtcNow;
        var hash = tokens.HashToken(command.Token);

        var user = await db.Users
            .Include(u => u.Tokens)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(
                u => u.Tokens.Any(t => t.TokenHash == hash && t.Purpose == TokenPurpose.ResetPassword),
                cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(Error.Validation(
                "token.invalid", "That link is no longer valid. Request a new one."));
        }

        var consumed = user.ConsumeToken(TokenPurpose.ResetPassword, hash, now);
        if (consumed.IsFailure)
        {
            return consumed;
        }

        // ChangePassword ends every session, including one an attacker may
        // already be holding. That is the whole value of a reset: regaining the
        // account means evicting everyone else from it, and a reset that left
        // existing sessions alive would leave the intruder signed in behind the
        // new password.
        user.ChangePassword(passwords.Hash(command.Password), now, wasReset: true);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/auth/reset-password", async (
            Command command, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(command, cancellationToken)).ToHttpResult())
        .WithName("ResetPassword")
        .WithSummary("Spend a reset link. Ends every session on success.")
        .WithTags("Identity")
        .ProducesValidationProblem()
        .RequireRateLimiting(RateLimits.Auth)
        .AllowAnonymous();
}
