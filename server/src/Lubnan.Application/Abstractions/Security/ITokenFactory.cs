using Lubnan.Domain.Users;

namespace Lubnan.Application.Abstractions.Security;

/// <summary>A refresh token: the value for the client, the hash for the row.</summary>
/// <remarks>
/// They are returned together because that is the only instant both exist. The
/// plaintext goes into a cookie and is never stored; the hash goes into the
/// database and is never sent.
/// </remarks>
public sealed record RefreshToken(string Value, string Hash);

/// <summary>Mints the credentials a signed-in session runs on.</summary>
public interface ITokenFactory
{
    /// <summary>
    /// A signed access token. Short-lived, validated without a database read.
    /// </summary>
    /// <remarks>
    /// Carries the user id, the security stamp and whether they administer.
    /// It deliberately does not carry the email address or the display name:
    /// a JWT is base64, not encryption, so everything in it is readable by
    /// anything that can see the cookie, and both of those change without the
    /// token changing.
    /// </remarks>
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);

    /// <summary>
    /// 256 bits from a cryptographic RNG, with its SHA-256 hash.
    /// </summary>
    /// <remarks>
    /// Not a GUID. <c>Guid.NewGuid</c> is a v4 GUID with 122 random bits from a
    /// source that is not documented as cryptographic, and it is the classic
    /// way to end up with guessable session tokens.
    /// <para>
    /// Hashed with SHA-256 rather than with the password hasher, because this
    /// is full-entropy random rather than something a person chose. There is no
    /// dictionary to attack, so a deliberately slow hash would only add latency
    /// to every refresh.
    /// </para>
    /// </remarks>
    RefreshToken CreateRefreshToken();

    /// <summary>Hash a token the client presented, so it can be looked up.</summary>
    string HashToken(string token);

    /// <summary>
    /// A single-use, time-limited code for confirming an address or resetting a
    /// password, with the hash to store.
    /// </summary>
    RefreshToken CreatePurposeToken();
}
