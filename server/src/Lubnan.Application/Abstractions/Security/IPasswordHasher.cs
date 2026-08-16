namespace Lubnan.Application.Abstractions.Security;

/// <summary>What happened when a password was checked.</summary>
public enum PasswordVerification
{
    Failed = 0,
    Succeeded = 1,

    /// <summary>
    /// Correct, but hashed with parameters we have since moved on from. The
    /// caller should re-hash and save, which is the only moment the plaintext
    /// is available to do it with.
    /// </summary>
    SucceededButNeedsRehash = 2,
}

/// <summary>
/// Turns passwords into hashes and back into yes-or-no.
/// </summary>
/// <remarks>
/// An interface over Microsoft's implementation rather than an implementation.
/// Nothing in this codebase writes a password hash by hand: the work factor,
/// the salt, the format version and the constant-time comparison are all
/// decisions with known-bad answers, and the known-good ones are already in the
/// framework.
/// <para>
/// The abstraction exists so the algorithm can be changed without touching a
/// handler, and so tests can hash instantly instead of paying the work factor
/// a hundred times.
/// </para>
/// </remarks>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Must take the same time whether or not the hash matches, and must be
    /// callable with a dummy hash so that a sign-in attempt against an address
    /// that does not exist costs the same as one against an address that does.
    /// </summary>
    PasswordVerification Verify(string hash, string password);
}
