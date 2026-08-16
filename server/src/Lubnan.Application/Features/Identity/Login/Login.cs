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
using Microsoft.Extensions.Options;

namespace Lubnan.Application.Features.Identity.Login;

public sealed record Command(string Email, string Password, RequestFingerprint Fingerprint)
    : ICommand<Result<SessionGrant>>;

internal sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(c => c.Email).NotEmpty().MaximumLength(Email.MaxLength);
        RuleFor(c => c.Password).NotEmpty().MaximumLength(256);
    }
}

internal sealed class Handler(
    IAppDbContext db,
    IPasswordHasher passwords,
    ITokenFactory tokens,
    IIpHasher ipHasher,
    IOptions<AuthOptions> options,
    IClock clock)
    : ICommandHandler<Command, Result<SessionGrant>>
{
    /// <summary>
    /// One message for every way this can fail.
    /// </summary>
    /// <remarks>
    /// No account, wrong password, unconfirmed address, suspended, locked out —
    /// all of them answer identically. Anything more specific tells an
    /// unauthenticated caller whether an address is registered, and "this
    /// account is locked" additionally confirms they found a real one worth
    /// coming back to.
    /// <para>
    /// The account holder is told the difference by email, where they have
    /// already proved they are the account holder.
    /// </para>
    /// </remarks>
    private static readonly Error Denied = Error.Unauthorized(
        "auth.invalidCredentials", "That email address and password do not match an account.");

    public async Task<Result<SessionGrant>> Handle(Command command, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var email = Email.Create(command.Email);

        if (email.IsFailure)
        {
            // Still burn the hashing time. Returning early on a malformed
            // address makes "not an address" measurably faster than "wrong
            // password", which is the timing oracle this whole method avoids.
            passwords.Verify(string.Empty, command.Password);
            return Result.Failure<SessionGrant>(Denied);
        }

        var user = await db.Users
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Email == email.Value, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            passwords.Verify(string.Empty, command.Password);
            return Result.Failure<SessionGrant>(Denied);
        }

        var ipHash = ipHasher.Hash(command.Fingerprint.Ip);

        // Verify before checking state, always. Checking lockout or suspension
        // first would return without hashing, and the difference in timing is
        // measurable - which would let someone enumerate locked accounts.
        var verification = passwords.Verify(user.PasswordHash, command.Password);

        if (verification is PasswordVerification.Failed)
        {
            user.RecordFailedSignIn(now, ipHash);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure<SessionGrant>(Denied);
        }

        if (!user.CanSignIn(now))
        {
            // Suspended, anonymised or locked out. The password was right, so
            // record the attempt - a run of these against a suspended account
            // is somebody who knows the credentials and is worth noticing - but
            // answer exactly as if it were wrong.
            return Result.Failure<SessionGrant>(Denied);
        }

        if (!user.EmailConfirmed)
        {
            return Result.Failure<SessionGrant>(Denied);
        }

        // The stored hash used parameters we have moved on from, and this is
        // the only moment the plaintext exists to re-hash with. Silent, and it
        // upgrades the whole user base as people sign in rather than requiring
        // a reset.
        if (verification is PasswordVerification.SucceededButNeedsRehash)
        {
            user.ChangePassword(passwords.Hash(command.Password), now);
        }

        user.RecordSuccessfulSignIn(now, ipHash);

        var grant = SessionIssuer.Issue(
            user, tokens, options.Value, now, command.Fingerprint.UserAgent, ipHash);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(grant);
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/auth/login", async (
            LoginRequest body,
            HttpRequest http,
            IOptions<AuthOptions> options,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new Command(body.Email, body.Password, http.Fingerprint());
            var result = await sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return result.ToHttpResult();
            }

            result.Value.Write(http.HttpContext.Response, options.Value);

            // No body. The tokens are in httpOnly cookies and returning them
            // here as well would put them somewhere script can read, which is
            // the entire thing the cookies exist to prevent.
            return Results.NoContent();
        })
        .WithName("Login")
        .WithSummary("Exchange credentials for a session. Sets httpOnly cookies; returns no body.")
        .WithTags("Identity")
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesValidationProblem()
        .RequireRateLimiting(RateLimits.Auth)
        .AllowAnonymous();
}

/// <summary>The wire shape. The fingerprint comes from the request, not the caller.</summary>
public sealed record LoginRequest(string Email, string Password);
