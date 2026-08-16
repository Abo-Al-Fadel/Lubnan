using Lubnan.Application.Abstractions.Security;
using Microsoft.AspNetCore.Identity;

namespace Lubnan.Infrastructure.Security;

/// <summary>
/// Password hashing, delegated to Microsoft's implementation.
/// </summary>
/// <remarks>
/// This class contains no cryptography, and that is its entire justification.
/// <c>PasswordHasher&lt;T&gt;</c> is PBKDF2-HMAC-SHA512 at 100,000 iterations
/// with a 128-bit random salt, a versioned output format so the work factor can
/// be raised later, and a fixed-time comparison. Every one of those has a
/// plausible-looking wrong answer, and getting any of them wrong is invisible
/// until it matters.
/// <para>
/// Only the <c>User</c> type parameter is unused — the framework class is
/// generic for an interface this codebase does not implement, so it is closed
/// over <see cref="object"/> and ignored.
/// </para>
/// </remarks>
internal sealed class PasswordHasher : IPasswordHasher
{
    /// <summary>
    /// A real hash of a value nobody knows, used to burn the same CPU time on a
    /// sign-in attempt for an address that does not exist as for one that does.
    /// </summary>
    /// <remarks>
    /// Without it, "no such user" returns in microseconds and "wrong password"
    /// returns in milliseconds, and the difference is measurable over the
    /// network. That turns the login endpoint into an oracle that confirms
    /// which addresses have accounts — which is the first step of every
    /// credential-stuffing run, and it works even though the endpoint's wording
    /// is careful.
    /// </remarks>
    private static readonly string DecoyHash =
        new PasswordHasher<object>().HashPassword(new object(), Guid.NewGuid().ToString());

    private readonly PasswordHasher<object> _inner = new();

    public string Hash(string password) => _inner.HashPassword(new object(), password);

    public PasswordVerification Verify(string hash, string password)
    {
        // An anonymised account has no hash. Verify against the decoy so the
        // timing still matches, and fail.
        if (string.IsNullOrEmpty(hash))
        {
            _inner.VerifyHashedPassword(new object(), DecoyHash, password);
            return PasswordVerification.Failed;
        }

        return _inner.VerifyHashedPassword(new object(), hash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Succeeded,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SucceededButNeedsRehash,
            _ => PasswordVerification.Failed,
        };
    }

    /// <summary>Burn the time without a user. Called when no account matched.</summary>
    public void VerifyDecoy(string password) =>
        _inner.VerifyHashedPassword(new object(), DecoyHash, password);
}
