using FluentValidation;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Lubnan.Domain.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.Moderation;

/// <summary>
/// Suspending an account and putting it back.
/// </summary>
/// <remarks>
/// Both operations are reversible and both record who did them. That is the
/// whole design: a suspension that cannot be undone with the same evidence
/// that justified it is not moderation, it is deletion with extra steps.
/// </remarks>
public sealed record SuspendCommand(Guid UserId, string Reason, DateTimeOffset? Until) : ICommand<Result>;

internal sealed class SuspendValidator : AbstractValidator<SuspendCommand>
{
    public SuspendValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();

        // The domain refuses an empty reason too. Validating here as well turns
        // it into a 400 with a field name rather than a 409, which is what it
        // actually is: a malformed request, not a conflicting one.
        RuleFor(c => c.Reason)
            .NotEmpty().WithMessage("A suspension has to say why.")
            .MaximumLength(500);
    }
}

internal sealed class SuspendHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    : ICommandHandler<SuspendCommand, Result>
{
    public async Task<Result> Handle(SuspendCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } actorId)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        // A moderator cannot suspend themselves. It reads like a joke until
        // somebody does it by pasting the wrong id and locks the only
        // administrator out of the tool that would undo it.
        if (command.UserId == actorId)
        {
            return Result.Failure(Error.Validation(
                "moderation.self", "You cannot suspend your own account."));
        }

        var user = await db.Users
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(Error.NotFound("user.notFound", "No such account."));
        }

        var result = user.Suspend(actorId, command.Reason, clock.UtcNow, command.Until);
        if (result.IsFailure)
        {
            return result;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

public sealed record ReinstateCommand(Guid UserId, string? Reason) : ICommand<Result>;

internal sealed class ReinstateHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    : ICommandHandler<ReinstateCommand, Result>
{
    public async Task<Result> Handle(ReinstateCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } actorId)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(Error.NotFound("user.notFound", "No such account."));
        }

        var result = user.Reinstate(actorId, command.Reason, clock.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // RequireAuthorization(CanModerate), not RequireAuthorization() plus a
        // role check in the handler. The policy is evaluated before the handler
        // is constructed, so an endpoint that forgets it fails closed at the
        // routing layer rather than depending on a line inside a method.
        app.MapPost("/api/v1/admin/users/{id:guid}/suspension", async (
                Guid id,
                SuspendBody body,
                ISender sender,
                CancellationToken cancellationToken) =>
                (await sender.Send(new SuspendCommand(id, body.Reason, body.Until), cancellationToken))
                    .ToHttpResult())
            .WithName("SuspendUser")
            .WithSummary("Block an account. Reversible, and records who and why.")
            .WithTags("Moderation")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(Policies.CanModerate)
            .RequireRateLimiting(RateLimits.Write);

        app.MapDelete("/api/v1/admin/users/{id:guid}/suspension", async (
                Guid id,
                string? reason,
                ISender sender,
                CancellationToken cancellationToken) =>
                (await sender.Send(new ReinstateCommand(id, reason), cancellationToken)).ToHttpResult())
            .WithName("ReinstateUser")
            .WithSummary("Undo a suspension, including one that should never have happened.")
            .WithTags("Moderation")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(Policies.CanModerate)
            .RequireRateLimiting(RateLimits.Write);
    }
}

/// <param name="Until">Null suspends indefinitely, pending review.</param>
public sealed record SuspendBody(string Reason, DateTimeOffset? Until);
