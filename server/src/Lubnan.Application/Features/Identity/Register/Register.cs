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

namespace Lubnan.Application.Features.Identity.Register;

public sealed record Command(string Email, string Password, string DisplayName) : ICommand<Result>;

internal sealed class Validator : AbstractValidator<Command>
{
    /// <summary>
    /// Length, and nothing else.
    /// </summary>
    /// <remarks>
    /// No "one uppercase, one digit, one symbol". Those rules push people
    /// towards <c>Password1!</c> — predictable, short, and in every cracking
    /// dictionary — while rejecting a long passphrase that is orders of
    /// magnitude stronger. Both NIST SP 800-63B and the NCSC now say the same
    /// thing: require length, allow everything, and check against known-breached
    /// passwords instead of inventing character classes.
    /// </remarks>
    public Validator()
    {
        RuleFor(c => c.Email).NotEmpty().MaximumLength(Email.MaxLength);

        RuleFor(c => c.Password)
            .MinimumLength(12).WithMessage("Use at least 12 characters. A short sentence works well.")

            // Hashing is deliberately slow, so an unbounded password is a way
            // to make the server do unbounded work — a handful of megabyte
            // passwords is a denial of service against one CPU each.
            .MaximumLength(256).WithMessage("That password is too long.");

        RuleFor(c => c.DisplayName)
            .NotEmpty()
            .MinimumLength(DisplayName.MinLength)
            .MaximumLength(DisplayName.MaxLength);
    }
}

internal sealed class Handler(
    IAppDbContext db,
    IPasswordHasher passwords,
    IBreachedPasswordCheck breached,
    IClock clock)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (email.IsFailure)
        {
            return email;
        }

        var displayName = DisplayName.Create(command.DisplayName);
        if (displayName.IsFailure)
        {
            return displayName;
        }

        // Checked before anything is written, so a breached password never
        // reaches the database even briefly.
        //
        // This completes the rule the validator starts. Length and no character
        // classes is only two thirds of what NIST and the NCSC actually say;
        // the third is "and check it against known breaches", without which a
        // twelve-character password can still be qwertyuiop12. Credential
        // stuffing does not guess - it replays lists, and this is the list.
        if (await breached.IsBreachedAsync(command.Password, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.Validation(
                "password.breached",
                "That password has appeared in a public data breach. Choose a different one."));
        }

        var now = clock.UtcNow;

        // Hash before the lookup, always. Skipping it on an address we already
        // know makes "that address is registered" measurably faster than a
        // new one, which is the timing oracle the identical HTTP response
        // exists to close.
        var passwordHash = passwords.Hash(command.Password);

        var existing = await db.Users
            .Include(u => u.AccountEvents.Where(e => e.Type == AccountEventType.RegistrationReattempted))
            .FirstOrDefaultAsync(u => u.Email == email.Value, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            // Success, on purpose, and this is the one piece of dishonesty in
            // the whole API.
            //
            // Answering "that address is already registered" turns this
            // endpoint into an oracle: anyone can test a list of addresses and
            // learn which of them have accounts here. On a site with a public
            // community feed that is a map from real names to real addresses.
            //
            // So registration always answers the same way, and the *email*
            // carries the difference: a new address gets a confirmation link,
            // an existing one gets "someone tried to register with your
            // address; here is how to sign in". The person who owns the
            // address learns everything; the person who does not learns
            // nothing.
            existing.NoteRegistrationAttempt(now);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }

        var user = User.Register(email.Value, displayName.Value, passwordHash, now);
        if (user.IsFailure)
        {
            return user;
        }

        db.Users.Add(user.Value);

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // The unique index on email is the real guard against two requests
            // racing. The loser still gets the same HTTP success as a new
            // signup — anything else would be an enumeration oracle. Drop the
            // failed insert, then treat this as a re-attempt so the owner
            // is told.
            db.Untrack(user.Value);

            var winner = await db.Users
                .Include(u => u.AccountEvents.Where(e => e.Type == AccountEventType.RegistrationReattempted))
                .FirstOrDefaultAsync(u => u.Email == email.Value, cancellationToken)
                .ConfigureAwait(false);

            if (winner is null)
            {
                throw;
            }

            winner.NoteRegistrationAttempt(now);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }

        // The confirmation mail is sent by a consumer of UserRegistered, from
        // the outbox. Sending it here would mean a registration that fails
        // because a mail provider was briefly down - after the account was
        // already created.
        return Result.Success();
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/auth/register", async (
            Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(command, cancellationToken)).ToHttpResult())
        .WithName("Register")
        .WithSummary("Create an account. Always answers the same way, whether or not the address is known.")
        .WithTags("Identity")
        .ProducesValidationProblem()
        .RequireRateLimiting(RateLimits.Auth)
        .AllowAnonymous();
}
