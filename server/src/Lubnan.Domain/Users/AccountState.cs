namespace Lubnan.Domain.Users;

/// <summary>
/// Where an account is in its life. Every transition is reversible except the
/// last, and the last only happens on a schedule.
/// </summary>
/// <remarks>
/// <code>
///                  ┌──────────────── reinstate ─────────────────┐
///                  ▼                                            │
///   (new) ──▶ Active ──── suspend ───▶ Suspended ───────────────┘
///                │ ▲
///     request    │ │ cancel, or simply log in
///     deletion   │ │ during the grace period
///                ▼ │
///          PendingDeletion ──── grace expires, background job ──▶ Anonymised
/// </code>
/// There is deliberately no transition that an HTTP request can make which
/// destroys data. <c>Anonymised</c> is reached only by a scheduled job, only
/// after the grace period, and even then the row survives — its personal data
/// does not.
/// </remarks>
public enum AccountState
{
    /// <summary>Normal. Can sign in, post, and be seen.</summary>
    Active = 0,

    /// <summary>
    /// Blocked by a moderator. Sessions are revoked and content is hidden, but
    /// nothing is destroyed: this is the state a wrongly-banned person is
    /// restored from, and it has to be losslessly reversible or the appeal is
    /// worthless.
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// The account holder asked to leave. Sessions are gone and content is
    /// hidden, but the grace period is still running and everything comes back
    /// if they change their mind — or if it was not them who asked.
    /// </summary>
    PendingDeletion = 2,

    /// <summary>
    /// The grace period expired and personal data has been overwritten. The row
    /// remains so that foreign keys, moderation history and the audit trail
    /// stay intact. This is the one state with no way back, which is why
    /// nothing but a scheduled job can reach it.
    /// </summary>
    Anonymised = 3,
}

/// <summary>Why something was hidden, so unhiding can be selective.</summary>
/// <remarks>
/// A post hidden because its author left must not be un-hidden by a moderator
/// clearing an unrelated report, and vice versa. Storing the reason is what
/// makes "restore what this event hid" answerable at all.
/// </remarks>
public enum HiddenReason
{
    None = 0,
    AuthorSuspended = 1,
    AuthorPendingDeletion = 2,
    Moderated = 3,
    AuthorChoice = 4,
}
