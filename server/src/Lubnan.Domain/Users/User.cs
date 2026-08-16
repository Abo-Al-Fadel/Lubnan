using Lubnan.Domain.Common;
using Lubnan.Domain.Users.Events;

namespace Lubnan.Domain.Users;

/// <summary>
/// A person with an account. The aggregate root for sign-in, sessions and the
/// whole account lifecycle.
/// </summary>
/// <remarks>
/// Not <c>IdentityUser</c>. ASP.NET Core Identity's entity is a persistence
/// shape, and inheriting it would put <c>Microsoft.AspNetCore.Identity</c> in
/// the Domain project — which the architecture tests forbid, and rightly: the
/// rules below are about accounts, not about how accounts are stored.
/// <para>
/// What is <em>not</em> hand-written is the cryptography. Password hashing
/// comes from Microsoft's <c>PasswordHasher</c> behind
/// <c>IPasswordHasher</c>, token bytes come from the platform CSPRNG. This
/// class decides <em>when</em> to hash and <em>what</em> a failure means; it
/// never decides how.
/// </para>
/// <para>
/// <b>Nothing here destroys anything.</b> Suspension, deletion and hiding are
/// all reversible states with a recorded reason, because the alternative —
/// a request that permanently removes data — has no answer to "we were wrong"
/// or to "that was not the account holder". The single irreversible transition,
/// <see cref="Anonymise"/>, is reachable only from
/// <see cref="AccountState.PendingDeletion"/>, only after the grace period, and
/// only by a scheduled job.
/// </para>
/// </remarks>
public sealed class User : AggregateRoot
{
    /// <summary>How long a departing account can change its mind.</summary>
    /// <remarks>
    /// Thirty days is the industry norm and it is chosen for one reason: an
    /// account deletion requested by somebody who is not the account holder has
    /// to be recoverable by a person who might be on holiday when it happens.
    /// </remarks>
    public static readonly TimeSpan DeletionGracePeriod = TimeSpan.FromDays(30);

    /// <summary>Failed attempts before the account stops answering.</summary>
    public const int MaxFailedSignIns = 10;

    /// <summary>How long a locked account stays locked.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<UserSession> _sessions = [];
    private readonly List<AccountEvent> _accountEvents = [];

    private User(Guid id, Email email, DisplayName displayName, string passwordHash, DateTimeOffset now)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        State = AccountState.Active;
        SecurityStamp = Guid.NewGuid();
        CreatedAt = now;
    }

    private User() { }

    public Email Email { get; private set; } = null!;

    public DisplayName DisplayName { get; private set; } = null!;

    /// <summary>Opaque to this class. Produced and verified by the hasher.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public bool EmailConfirmed { get; private set; }

    public AccountState State { get; private set; }

    public bool IsAdmin { get; private set; }

    /// <summary>
    /// Changes whenever every existing session must stop being trusted: a
    /// password change, a sign-out-everywhere, a suspension.
    /// </summary>
    /// <remarks>
    /// It rides in the access token as a claim. Access tokens are short-lived
    /// and validated without touching the database, so a revoked session keeps
    /// working until its access token expires — a bounded window, currently
    /// fifteen minutes, and the price of stateless validation. The refresh side
    /// is stateful and stops immediately.
    /// <para>
    /// Having the claim in the token from the first release is what makes the
    /// upgrade — checking the stamp against a cached value on every request —
    /// a change to validation rather than a change to the token format that
    /// would sign every user out.
    /// </para>
    /// </remarks>
    public Guid SecurityStamp { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? LastSignedInAt { get; private set; }

    public int FailedSignInCount { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    // ── Suspension ──────────────────────────────────────────────────────────
    public DateTimeOffset? SuspendedAt { get; private set; }

    /// <summary>Null while suspended indefinitely.</summary>
    public DateTimeOffset? SuspendedUntil { get; private set; }

    public string? SuspensionReason { get; private set; }

    // ── Departure ───────────────────────────────────────────────────────────
    public DateTimeOffset? DeletionRequestedAt { get; private set; }

    public DateTimeOffset? PurgeAfter { get; private set; }

    public DateTimeOffset? AnonymisedAt { get; private set; }

    public IReadOnlyList<UserSession> Sessions => _sessions.AsReadOnly();

    public IReadOnlyList<AccountEvent> AccountEvents => _accountEvents.AsReadOnly();

    /// <summary>Can this account sign in and act right now?</summary>
    public bool CanSignIn(DateTimeOffset now) =>
        State is AccountState.Active or AccountState.PendingDeletion
        && !IsLockedOut(now);

    public bool IsLockedOut(DateTimeOffset now) => LockedUntil is { } until && now < until;

    /// <summary>True while the account still has a way back.</summary>
    public bool IsRecoverable => State is AccountState.Suspended or AccountState.PendingDeletion;

    public static Result<User> Register(
        Email email,
        DisplayName displayName,
        string passwordHash,
        DateTimeOffset now)
    {
        var user = new User(Guid.NewGuid(), email, displayName, passwordHash, now);
        user.Record(AccountEventType.Registered, now);
        user.Raise(new UserRegistered(user.Id, email.Value, displayName.Value));
        return Result.Success(user);
    }

    // ── Email ───────────────────────────────────────────────────────────────

    public Result ConfirmEmail(DateTimeOffset now)
    {
        // Idempotent. A confirmation link gets clicked twice — by the reader,
        // by their mail client prefetching it, by a corporate link scanner —
        // and the second click must not be an error page.
        if (EmailConfirmed)
        {
            return Result.Success();
        }

        EmailConfirmed = true;
        Record(AccountEventType.EmailConfirmed, now);
        Raise(new UserEmailConfirmed(Id, Email.Value));
        return Result.Success();
    }

    /// <summary>
    /// Move to a new address. The caller must already have proved control of
    /// it; this only records the change.
    /// </summary>
    public Result ChangeEmail(Email newEmail, DateTimeOffset now)
    {
        if (Email == newEmail)
        {
            return Result.Success();
        }

        Email = newEmail;

        // The new address is confirmed by construction — the flow that gets
        // here required a link sent to it — but every existing session dies
        // anyway. Email is the recovery channel, so changing it is exactly the
        // move an attacker makes to lock the owner out, and the owner's other
        // devices must not keep working through it.
        EmailConfirmed = true;
        EndAllSessions(SessionEndReason.SignedOutEverywhere, now);
        SecurityStamp = Guid.NewGuid();

        Record(AccountEventType.EmailChanged, now);
        Raise(new UserEmailChanged(Id, newEmail.Value));
        return Result.Success();
    }

    // ── Passwords ───────────────────────────────────────────────────────────

    /// <summary>
    /// Replace the password and invalidate every session, including the one
    /// that made the change.
    /// </summary>
    /// <remarks>
    /// Signing the current device out too is deliberate. If the password was
    /// changed because it may have been exposed, leaving any session alive
    /// defeats the point — and the attacker's session is indistinguishable
    /// from the owner's.
    /// </remarks>
    public Result ChangePassword(string newPasswordHash, DateTimeOffset now, bool wasReset = false)
    {
        PasswordHash = newPasswordHash;
        SecurityStamp = Guid.NewGuid();
        FailedSignInCount = 0;
        LockedUntil = null;

        EndAllSessions(SessionEndReason.SignedOutEverywhere, now);

        Record(wasReset ? AccountEventType.PasswordReset : AccountEventType.PasswordChanged, now);
        Raise(new UserPasswordChanged(Id, Email.Value, wasReset));
        return Result.Success();
    }

    /// <summary>Record a failed attempt and lock out once they add up.</summary>
    public void RecordFailedSignIn(DateTimeOffset now, string? ipHash = null)
    {
        FailedSignInCount++;
        Record(AccountEventType.SignInFailed, now, ipHash: ipHash);

        if (FailedSignInCount < MaxFailedSignIns)
        {
            return;
        }

        // Temporary, not permanent. A permanent lock on failed attempts hands
        // anyone who knows an address the ability to lock its owner out, which
        // converts a guessing attack into a denial-of-service that works.
        LockedUntil = now + LockoutDuration;
        FailedSignInCount = 0;
        Record(AccountEventType.SignInBlocked, now, ipHash: ipHash);
        Raise(new UserLockedOut(Id, Email.Value, LockedUntil.Value));
    }

    public void RecordSuccessfulSignIn(DateTimeOffset now, string? ipHash = null)
    {
        FailedSignInCount = 0;
        LockedUntil = null;
        LastSignedInAt = now;
        Record(AccountEventType.SignedIn, now, ipHash: ipHash);
    }

    // ── Sessions ────────────────────────────────────────────────────────────

    /// <summary>Begin a new session. A fresh family, because this is a new sign-in.</summary>
    public UserSession StartSession(
        string tokenHash,
        DateTimeOffset now,
        TimeSpan lifetime,
        string? userAgent = null,
        string? ipHash = null)
    {
        var session = UserSession.Start(Id, Guid.NewGuid(), tokenHash, now, lifetime, userAgent, ipHash);
        _sessions.Add(session);
        return session;
    }

    /// <summary>
    /// Exchange a live refresh token for a new one, keeping the family.
    /// </summary>
    /// <remarks>
    /// Rotation on every refresh is what makes theft <em>detectable</em>. It
    /// does not prevent it: a stolen token works once. But the moment either
    /// party uses the old token afterwards, <see cref="DetectReuse"/> fires,
    /// and the legitimate holder finds out because they were signed out.
    /// </remarks>
    public Result<UserSession> Rotate(
        UserSession current,
        string newTokenHash,
        DateTimeOffset now,
        TimeSpan lifetime,
        string? userAgent = null,
        string? ipHash = null)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (!current.IsUsable(now))
        {
            return Result.Failure<UserSession>(Error.Unauthorized(
                "session.expired", "That session has ended. Sign in again."));
        }

        var successor = UserSession.Start(Id, current.FamilyId, newTokenHash, now, lifetime, userAgent, ipHash);
        _sessions.Add(successor);
        current.RotateInto(successor, now);

        return Result.Success(successor);
    }

    /// <summary>
    /// An already-rotated token came back. Revoke its entire family.
    /// </summary>
    /// <remarks>
    /// Two things can cause this: the token was stolen and replayed, or the
    /// legitimate client retried a refresh whose response it never received.
    /// They are indistinguishable from here, so the safe reading wins — and the
    /// cost of being wrong is one unexpected sign-in, against the cost of being
    /// right and doing nothing, which is a silent takeover.
    /// <para>
    /// The family, not just the token. By the time reuse shows up the thief may
    /// have rotated several times, so revoking the presented token alone leaves
    /// their current one working.
    /// </para>
    /// </remarks>
    public Result DetectReuse(UserSession reused, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(reused);

        var family = _sessions.Where(s => s.FamilyId == reused.FamilyId && s.IsActive).ToList();

        foreach (var session in family)
        {
            session.End(SessionEndReason.ReuseDetected, now);
        }

        Record(AccountEventType.RefreshReuseDetected, now, reason: $"family {reused.FamilyId}");
        Raise(new RefreshTokenReuseDetected(Id, Email.Value, reused.FamilyId, family.Count));

        return Result.Failure(Error.Unauthorized(
            "session.reuse",
            "That session has been ended for security reasons. Sign in again."));
    }

    public Result EndSession(Guid sessionId, DateTimeOffset now)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);

        if (session is null || !session.IsActive)
        {
            // Not found and already ended answer the same way. A distinguishable
            // 404 would let anyone holding one session enumerate the ids of the
            // others.
            return Result.Success();
        }

        session.End(SessionEndReason.SignedOut, now);
        Record(AccountEventType.SessionRevoked, now);
        return Result.Success();
    }

    /// <summary>The button that matters after "was that you?".</summary>
    public Result SignOutEverywhere(DateTimeOffset now)
    {
        EndAllSessions(SessionEndReason.SignedOutEverywhere, now);
        SecurityStamp = Guid.NewGuid();
        Record(AccountEventType.SignedOutEverywhere, now);
        Raise(new UserSignedOutEverywhere(Id, Email.Value));
        return Result.Success();
    }

    // ── Suspension ──────────────────────────────────────────────────────────

    /// <summary>
    /// Block an account. Reversible without loss, always.
    /// </summary>
    /// <param name="until">Null suspends indefinitely, pending review.</param>
    public Result Suspend(Guid actorId, string reason, DateTimeOffset now, DateTimeOffset? until = null)
    {
        if (State is AccountState.Anonymised)
        {
            return Result.Failure(Error.Conflict(
                "account.anonymised", "That account no longer exists."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            // A suspension with no reason cannot be reviewed, appealed or
            // undone with any confidence. Requiring one is not bureaucracy;
            // it is what makes the action reversible by someone else later.
            return Result.Failure(Error.Validation(
                "suspension.reason", "A suspension has to say why."));
        }

        State = AccountState.Suspended;
        SuspendedAt = now;
        SuspendedUntil = until;
        SuspensionReason = reason.Trim();

        EndAllSessions(SessionEndReason.AccountClosed, now);
        SecurityStamp = Guid.NewGuid();

        Record(AccountEventType.Suspended, now, actorId, reason);

        // Content is hidden, not deleted, and it carries the reason so that
        // reinstating restores exactly what this suspension hid.
        Raise(new UserSuspended(Id, Email.Value, actorId, reason, until));
        return Result.Success();
    }

    /// <summary>
    /// Undo a suspension, including one that should never have happened.
    /// </summary>
    public Result Reinstate(Guid actorId, string? reason, DateTimeOffset now)
    {
        if (State is not AccountState.Suspended)
        {
            return Result.Failure(Error.Conflict(
                "account.notSuspended", "That account is not suspended."));
        }

        State = AccountState.Active;
        SuspendedAt = null;
        SuspendedUntil = null;
        SuspensionReason = null;

        Record(AccountEventType.Reinstated, now, actorId, reason);
        Raise(new UserReinstated(Id, Email.Value, actorId));
        return Result.Success();
    }

    // ── Departure ───────────────────────────────────────────────────────────

    /// <summary>
    /// Start the clock on leaving. Nothing is destroyed today.
    /// </summary>
    /// <remarks>
    /// The account stops being usable immediately — sessions end, content is
    /// hidden — because that is what somebody deleting their account expects to
    /// see. But the data survives the grace period, and
    /// <see cref="CancelDeletion"/> puts it all back.
    /// <para>
    /// The grace period exists for the case where the request did not come from
    /// the account holder. A compromised account that deletes itself is
    /// recoverable; an account that is deleted synchronously is not, and no
    /// amount of authentication on the delete endpoint changes that.
    /// </para>
    /// </remarks>
    public Result RequestDeletion(DateTimeOffset now, Guid? actorId = null)
    {
        if (State is AccountState.Anonymised)
        {
            return Result.Failure(Error.Conflict("account.anonymised", "That account no longer exists."));
        }

        if (State is AccountState.PendingDeletion)
        {
            return Result.Success();
        }

        State = AccountState.PendingDeletion;
        DeletionRequestedAt = now;
        PurgeAfter = now + DeletionGracePeriod;

        EndAllSessions(SessionEndReason.AccountClosed, now);
        SecurityStamp = Guid.NewGuid();

        Record(AccountEventType.DeletionRequested, now, actorId);

        // Sends the "your account will be deleted on <date>, press here if this
        // was not you" mail. That message is the actual defence against a
        // hijacked account being erased.
        Raise(new UserDeletionRequested(Id, Email.Value, PurgeAfter.Value));
        return Result.Success();
    }

    /// <summary>Change of mind, or it was never their intention. Everything returns.</summary>
    public Result CancelDeletion(DateTimeOffset now, Guid? actorId = null)
    {
        if (State is AccountState.Anonymised)
        {
            return Result.Failure(Error.Conflict(
                "account.anonymised",
                "That account has passed its recovery period and cannot be restored."));
        }

        if (State is not AccountState.PendingDeletion)
        {
            return Result.Success();
        }

        State = AccountState.Active;
        DeletionRequestedAt = null;
        PurgeAfter = null;

        Record(AccountEventType.DeletionCancelled, now, actorId);
        Raise(new UserDeletionCancelled(Id, Email.Value));
        return Result.Success();
    }

    /// <summary>
    /// The one irreversible step. Overwrites personal data and keeps the row.
    /// </summary>
    /// <remarks>
    /// Reachable only from <see cref="AccountState.PendingDeletion"/>, only
    /// once <see cref="PurgeAfter"/> has passed, and in practice only from the
    /// scheduled job that looks for both. No HTTP request reaches it.
    /// <para>
    /// The row survives on purpose. Deleting it would cascade into posts,
    /// reactions and the audit trail — destroying moderation history and other
    /// people's conversations along with one person's data. Erasing the
    /// personal fields satisfies the obligation; the empty shell keeps every
    /// foreign key valid.
    /// </para>
    /// </remarks>
    /// <param name="emailTombstone">
    /// A one-way hash of the old address, so a returning person can be told
    /// "this address was used before" without the address being recoverable.
    /// </param>
    public Result Anonymise(string emailTombstone, DateTimeOffset now)
    {
        if (State is not AccountState.PendingDeletion)
        {
            return Result.Failure(Error.Conflict(
                "account.notPendingDeletion",
                "Only an account that asked to be deleted can be anonymised."));
        }

        if (PurgeAfter is null || now < PurgeAfter)
        {
            return Result.Failure(Error.Conflict(
                "account.stillRecoverable",
                $"That account is recoverable until {PurgeAfter:u}."));
        }

        Email = Email.Create($"{emailTombstone}@deleted.invalid").Value;
        DisplayName = DisplayName.Create("Deleted account").Value;
        PasswordHash = string.Empty;
        EmailConfirmed = false;
        IsAdmin = false;
        State = AccountState.Anonymised;
        AnonymisedAt = now;
        SecurityStamp = Guid.NewGuid();

        _sessions.Clear();

        Record(AccountEventType.Anonymised, now);
        Raise(new UserAnonymised(Id));
        return Result.Success();
    }

    // ── Roles ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Granted out of band — a migration or a console command, never an
    /// endpoint. An API that can promote an administrator is an API where one
    /// compromised administrator is all of them.
    /// </summary>
    public void GrantAdmin() => IsAdmin = true;

    public void RevokeAdmin() => IsAdmin = false;

    // ── Internals ───────────────────────────────────────────────────────────

    private void EndAllSessions(SessionEndReason reason, DateTimeOffset now)
    {
        foreach (var session in _sessions.Where(s => s.IsActive))
        {
            session.End(reason, now);
        }
    }

    private void Record(
        AccountEventType type,
        DateTimeOffset now,
        Guid? actorId = null,
        string? reason = null,
        string? ipHash = null) =>
        _accountEvents.Add(AccountEvent.Record(Id, type, now, actorId, reason, ipHash));
}
