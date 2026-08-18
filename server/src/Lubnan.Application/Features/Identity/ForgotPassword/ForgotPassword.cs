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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lubnan.Application.Features.Identity.ForgotPassword;

public sealed record Command(string Email) : ICommand<Result>;

internal sealed class Validator : AbstractValidator<Command>
{
    public Validator() => RuleFor(c => c.Email).NotEmpty().MaximumLength(Email.MaxLength);
}

internal sealed class Handler(
    IAppDbContext db,
    ITokenFactory tokens,
    IEmailSender mail,
    IOptions<AuthOptions> options,
    ILogger<Handler> logger,
    IClock clock)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var email = Email.Create(command.Email);

        // Always 204, whatever happened.
        //
        // A malformed address, one with no account, and one with an account all
        // answer identically. Otherwise this endpoint tells anyone who asks
        // which addresses are registered — the same oracle that registration
        // and sign-in are careful to close, and shutting two of three doors
        // shuts none.
        if (email.IsFailure)
        {
            return Result.Success();
        }

        var user = await db.Users
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(u => u.Email == email.Value, cancellationToken)
            .ConfigureAwait(false);

        // A suspended or departing account gets no reset link. Sending one
        // would hand an attacker a route into an account whose owner has
        // already been locked out, or one that is mid-deletion.
        if (user is null || user.State is not AccountState.Active || !user.EmailConfirmed)
        {
            return Result.Success();
        }

        var token = tokens.CreatePurposeToken();

        // Only the hash is stored. Issuing supersedes any earlier live reset
        // token, so pressing "send it again" six times does not leave six valid
        // ways in, each as old as the first request.
        user.IssueToken(TokenPurpose.ResetPassword, token.Hash, now, UserToken.ResetPasswordLifetime);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Sent from here rather than through the outbox, and this is the one
        // place that is right. The mail body has to carry the token itself, and
        // the outbox marks rows processed without deleting them — so routing
        // this through it would leave a working password-reset credential
        // sitting in a table indefinitely. Held in memory, sent once, never
        // written down.
        var web = options.Value.WebBaseUrl.TrimEnd('/');
        var link = web + "/reset-password?token=" + Uri.EscapeDataString(token.Value);

        var body =
            "Someone asked to reset the password for this address.\n\n"
            + link
            + "\n\nThe link works once and expires in an hour. If this was not you,\n"
            + "nothing has changed and you can ignore this message.\n";

        try
        {
            await mail.SendAsync(
                new OutgoingEmail(user.Email.Value, "Reset your Lubnan password", body),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Swallowed on purpose. A 500 when the mail provider is down would
            // answer differently for an address that has an account than for
            // one that does not, reopening through the error path exactly the
            // oracle the 204 above exists to close.
            logger.ResetMailFailed(ex);
        }

        return Result.Success();
    }
}

internal static partial class HandlerLog
{
    [LoggerMessage(EventId = 4100, Level = LogLevel.Error, Message = "Password reset mail could not be sent.")]
    public static partial void ResetMailFailed(this ILogger logger, Exception exception);
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/v1/auth/forgot-password", async (
            Command command, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(command, cancellationToken)).ToHttpResult())
        .WithName("ForgotPassword")
        .WithSummary("Request a reset link. Always answers 204, whether or not the address is known.")
        .WithTags("Identity")
        .RequireRateLimiting(RateLimits.Auth)
        .AllowAnonymous();
}
