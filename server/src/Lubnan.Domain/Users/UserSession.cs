using Lubnan.Domain.Common;

namespace Lubnan.Domain.Users;

/// <summary>Why a session ended.</summary>
public enum SessionEndReason
{
    Active = 0,

    /// <summary>Rotated normally: the holder refreshed and got a new token.</summary>
    Rotated = 1,

    /// <summary>The user signed out of this device.</summary>
    SignedOut = 2,

    /// <summary>The user signed out everywhere, or changed their password.</summary>
    SignedOutEverywhere = 3,

    /// <summary>
    /// An already-rotated token was presented again. Either it was stolen, or
    /// the legitimate holder is replaying — and there is no way to tell which,
    /// so the whole family goes.
    /// </summary>
    ReuseDetected = 4,

    /// <summary>A moderator suspended the account, or the holder is leaving.</summary>
    AccountClosed = 5,
}

/// <summary>
/// One refresh token, which is to say one signed-in device.
/// </summary>
/// <remarks>
/// Three things about this type are security decisions rather than modelling
/// ones.
/// <para>
/// <b>The token is stored hashed.</b> A refresh token is a bearer credential —
/// whoever holds it can mint access tokens. Storing it in plaintext means a
/// read-only leak of this table, from a backup or an injection, is a complete
/// account takeover for every signed-in user. Hashed, the same leak yields
/// nothing usable. It is hashed with SHA-256 rather than a password hash
/// because it is 256 bits of entropy from a CSPRNG, not a guessable secret:
/// there is no dictionary to attack, so the slowness that protects a password
/// would only cost latency on every refresh.
/// </para>
/// <para>
/// <b>Sessions belong to a family.</b> Refreshing rotates the token and links
/// the new session to the old one. That chain is what makes theft detectable.
/// </para>
/// <para>
/// <b>The IP is stored hashed too.</b> The profile page shows "signed in from a
/// new location"; that needs comparison, not the address itself. A raw address
/// history is personal data with a retention obligation, kept for a feature
/// that never needed it.
/// </para>
/// </remarks>
public sealed class UserSession : Entity
{
    private UserSession(
        Guid id,
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        string? userAgent,
        string? ipHash) : base(id)
    {
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        UserAgent = userAgent;
        IpHash = ipHash;
    }

    private UserSession() { }

    public Guid UserId { get; private init; }

    /// <summary>
    /// Shared by every token descended from one sign-in. Revoking a family is
    /// what happens when one of its tokens turns out to have been stolen.
    /// </summary>
    public Guid FamilyId { get; private init; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public DateTimeOffset? EndedAt { get; private set; }

    public SessionEndReason EndReason { get; private set; }

    /// <summary>Set when this session was rotated, so the chain can be walked.</summary>
    public Guid? ReplacedBy { get; private set; }

    /// <summary>Truncated, for "Chrome on Windows" in the session list.</summary>
    public string? UserAgent { get; private set; }

    public string? IpHash { get; private set; }

    /// <summary>Last time this session was used to refresh. Drives "last active".</summary>
    public DateTimeOffset? LastUsedAt { get; private set; }

    public bool IsActive => EndedAt is null;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsUsable(DateTimeOffset now) => IsActive && !IsExpired(now);

    internal static UserSession Start(
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset now,
        TimeSpan lifetime,
        string? userAgent,
        string? ipHash) =>
        new(Guid.NewGuid(), userId, familyId, tokenHash, now, now + lifetime, Truncate(userAgent), ipHash);

    internal void End(SessionEndReason reason, DateTimeOffset now)
    {
        // Idempotent. Ending an already-ended session must not overwrite why it
        // ended: if a family is revoked for reuse, the sign-out that follows
        // should not relabel the evidence as an ordinary sign-out.
        if (!IsActive)
        {
            return;
        }

        EndedAt = now;
        EndReason = reason;
    }

    internal void RotateInto(UserSession successor, DateTimeOffset now)
    {
        LastUsedAt = now;
        ReplacedBy = successor.Id;
        End(SessionEndReason.Rotated, now);
    }

    // A user agent is attacker-controlled and unbounded. It is displayed back
    // to the account holder, so it is capped here rather than trusted to be
    // reasonable.
    private static string? Truncate(string? userAgent) =>
        string.IsNullOrWhiteSpace(userAgent) ? null
        : userAgent.Length <= 256 ? userAgent
        : userAgent[..256];
}
