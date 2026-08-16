using Lubnan.Domain.Common;

namespace Lubnan.Domain.Users.Events;

/// <summary>
/// What happened to an account, for anything that is not the account.
/// </summary>
/// <remarks>
/// These carry the email address because their main consumer sends mail, and a
/// consumer that had to load the user to find the address would be reading a
/// row that may have changed since the event was raised. That is the point of
/// an event: it describes the world at the moment it happened.
/// <para>
/// None of them carries a password, a token or a hash. They go to the outbox,
/// which is a table with a longer retention and a wider audience than the
/// account itself.
/// </para>
/// </remarks>
public sealed record UserRegistered(Guid UserId, string Email, string DisplayName) : DomainEvent;

public sealed record UserEmailConfirmed(Guid UserId, string Email) : DomainEvent;

public sealed record UserEmailChanged(Guid UserId, string Email) : DomainEvent;

/// <param name="WasReset">
/// True when it came from a reset link rather than from someone who knew the
/// old password. The notification wording differs, and so does what the reader
/// should do if it was not them.
/// </param>
public sealed record UserPasswordChanged(Guid UserId, string Email, bool WasReset) : DomainEvent;

public sealed record UserLockedOut(Guid UserId, string Email, DateTimeOffset Until) : DomainEvent;

public sealed record UserSignedOutEverywhere(Guid UserId, string Email) : DomainEvent;

/// <summary>
/// A stolen refresh token was replayed. Worth a mail to the account holder and
/// a metric an operator can alert on: one of these is noise, a hundred in an
/// hour is an incident.
/// </summary>
public sealed record RefreshTokenReuseDetected(
    Guid UserId,
    string Email,
    Guid FamilyId,
    int SessionsRevoked) : DomainEvent;

/// <summary>
/// Consumers hide this account's content with
/// <c>HiddenReason.AuthorSuspended</c>, so reinstating can unhide exactly what
/// this hid and nothing a moderator hid separately.
/// </summary>
public sealed record UserSuspended(
    Guid UserId,
    string Email,
    Guid ActorId,
    string Reason,
    DateTimeOffset? Until) : DomainEvent;

public sealed record UserReinstated(Guid UserId, string Email, Guid ActorId) : DomainEvent;

/// <param name="PurgeAfter">
/// When the grace period ends. It goes in the mail, because "press here if this
/// was not you, before the 14th" is the actual defence against a hijacked
/// account erasing itself.
/// </param>
public sealed record UserDeletionRequested(Guid UserId, string Email, DateTimeOffset PurgeAfter) : DomainEvent;

public sealed record UserDeletionCancelled(Guid UserId, string Email) : DomainEvent;

/// <summary>
/// Carries no address, because by the time it is raised there is no longer one
/// to carry.
/// </summary>
public sealed record UserAnonymised(Guid UserId) : DomainEvent;
