namespace Lubnan.Application.Abstractions.Security;

/// <summary>
/// Has this password already appeared in a public breach?
/// </summary>
/// <remarks>
/// The rule this completes: NIST SP 800-63B and the NCSC both say to require
/// length, allow every character, and check the result against known-breached
/// passwords — rather than inventing character classes that push people towards
/// <c>Password1!</c>. The first two halves are in the validator. This is the
/// third, and without it the advice is only two-thirds taken.
/// <para>
/// It matters more than a strength meter. A twelve-character password can be
/// perfectly random or it can be <c>qwertyuiop12</c>, and only a corpus of real
/// breaches can tell the difference. Credential stuffing does not guess; it
/// replays lists.
/// </para>
/// </remarks>
public interface IBreachedPasswordCheck
{
    /// <summary>
    /// True when the password is known to have been breached.
    /// </summary>
    /// <remarks>
    /// Must <b>fail open</b>: if the service is unreachable, slow, or answers
    /// with nonsense, this returns false and registration proceeds. A
    /// third-party outage is not a reason to stop people opening accounts, and
    /// an implementation that threw here would make an external dependency a
    /// hard requirement of signing up.
    /// </remarks>
    Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken = default);
}
