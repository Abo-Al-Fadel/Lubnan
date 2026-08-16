using Lubnan.Domain.Common;

namespace Lubnan.Domain.Users;

/// <summary>What a one-time token is for. A token issued for one purpose must never work for another.</summary>
public enum TokenPurpose
{
    ConfirmEmail = 0,
    ResetPassword = 1,
    ChangeEmail = 2,
}

/// <summary>
/// A single-use, time-limited code sent by email.
/// </summary>
/// <remarks>
/// Stored hashed, exactly like a refresh token and for the same reason: a reset
/// token is a bearer credential that changes a password, so a read-only leak of
/// this table would be worse than a leak of the password hashes.
/// <para>
/// Single use is enforced by <see cref="ConsumedAt"/> rather than by deleting
/// the row. A consumed token that is presented again should be recognisably
/// spent — that is a signal worth logging — and a deleted row is
/// indistinguishable from one that never existed.
/// </para>
/// <para>
/// The purpose is part of the lookup. Without it, a token issued to confirm an
/// address would also reset the password, and email confirmation links are
/// forwarded, logged by mail scanners and pasted into chats far more casually
/// than reset links are.
/// </para>
/// </remarks>
public sealed class UserToken : Entity
{
    /// <summary>Long enough to survive a slow mail relay, short enough to matter.</summary>
    public static readonly TimeSpan ConfirmEmailLifetime = TimeSpan.FromDays(3);

    /// <summary>Deliberately shorter. It changes a password.</summary>
    public static readonly TimeSpan ResetPasswordLifetime = TimeSpan.FromHours(1);

    private UserToken(
        Guid id,
        Guid userId,
        TokenPurpose purpose,
        string tokenHash,
        string? payload,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt) : base(id)
    {
        UserId = userId;
        Purpose = purpose;
        TokenHash = tokenHash;
        Payload = payload;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
    }

    private UserToken() { }

    public Guid UserId { get; private init; }

    public TokenPurpose Purpose { get; private init; }

    public string TokenHash { get; private init; } = string.Empty;

    /// <summary>
    /// The pending value, when the token carries one. For a
    /// <see cref="TokenPurpose.ChangeEmail"/> this is the address being moved
    /// to, held here rather than on the user so that an unconfirmed change
    /// cannot lock anybody out of the address they still have.
    /// </summary>
    public string? Payload { get; private init; }

    public DateTimeOffset IssuedAt { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public bool IsUsable(DateTimeOffset now) => ConsumedAt is null && now < ExpiresAt;

    internal static UserToken Issue(
        Guid userId,
        TokenPurpose purpose,
        string tokenHash,
        DateTimeOffset now,
        TimeSpan lifetime,
        string? payload = null) =>
        new(Guid.NewGuid(), userId, purpose, tokenHash, payload, now, now + lifetime);

    internal void Consume(DateTimeOffset now) => ConsumedAt ??= now;
}
