using Lubnan.Domain.Common;

namespace Lubnan.Domain.Users;

/// <summary>Something that happened to an account and must stay on the record.</summary>
public enum AccountEventType
{
    Registered = 0,
    EmailConfirmed = 1,
    SignedIn = 2,
    SignInFailed = 3,
    SignInBlocked = 4,
    PasswordChanged = 5,
    PasswordResetRequested = 6,
    PasswordReset = 7,
    EmailChangeRequested = 8,
    EmailChanged = 9,
    SignedOutEverywhere = 10,
    SessionRevoked = 11,
    RefreshReuseDetected = 12,
    Suspended = 13,
    Reinstated = 14,
    DeletionRequested = 15,
    DeletionCancelled = 16,
    Anonymised = 17,
    RegistrationReattempted = 18,
}

/// <summary>
/// The audit trail. Append-only, and the reason recovery is possible at all.
/// </summary>
/// <remarks>
/// This is the answer to "someone got in and deleted everything". Nothing in
/// the application can update or delete a row here — the EF configuration
/// permits insert alone, and the database revokes the rest. So even total
/// control of an account cannot rewrite what was done with it, and the
/// sequence of events is what an operator restores from.
/// <para>
/// It is also the answer to "we suspended the wrong person". Every state
/// change records who made it, when, and why, so a suspension can be reversed
/// with the same evidence that justified it.
/// </para>
/// <para>
/// What it must never contain: passwords, tokens, reset codes, or the contents
/// of anything. An audit log is read by more people than the database is, and
/// it is retained for longer.
/// </para>
/// </remarks>
public sealed class AccountEvent : Entity
{
    private AccountEvent(
        Guid id,
        Guid userId,
        AccountEventType type,
        Guid? actorId,
        string? reason,
        string? ipHash,
        DateTimeOffset occurredAt) : base(id)
    {
        UserId = userId;
        Type = type;
        ActorId = actorId;
        Reason = reason;
        IpHash = ipHash;
        OccurredAt = occurredAt;
    }

    private AccountEvent() { }

    /// <summary>The account this happened to.</summary>
    public Guid UserId { get; private init; }

    public AccountEventType Type { get; private init; }

    /// <summary>
    /// Who did it. Null means the account holder acted on themselves or the
    /// system did; a different id means a moderator, and that distinction is
    /// the whole point of recording it.
    /// </summary>
    public Guid? ActorId { get; private init; }

    /// <summary>Free text from a moderator. Never contains user content.</summary>
    public string? Reason { get; private init; }

    public string? IpHash { get; private init; }

    public DateTimeOffset OccurredAt { get; private init; }

    internal static AccountEvent Record(
        Guid userId,
        AccountEventType type,
        DateTimeOffset occurredAt,
        Guid? actorId = null,
        string? reason = null,
        string? ipHash = null) =>
        new(Guid.NewGuid(), userId, type, actorId, Trim(reason), ipHash, occurredAt);

    private static string? Trim(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? null
        : reason.Trim().Length <= 500 ? reason.Trim()
        : reason.Trim()[..500];
}
