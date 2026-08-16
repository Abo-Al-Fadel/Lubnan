using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Lubnan.Infrastructure.Security;

/// <summary>Hashes for things that are compared but never read back.</summary>
internal static class Hashing
{
    /// <summary>
    /// A keyed hash of an IP address, for "signed in from a new location".
    /// </summary>
    /// <remarks>
    /// Keyed rather than plain, because the IPv4 space is small enough to
    /// enumerate completely: an unkeyed SHA-256 of an address is reversible in
    /// minutes by hashing all four billion, which makes it storage of the
    /// address with extra steps.
    /// <para>
    /// Truncated to sixteen bytes. Comparison is all this is for, and a shorter
    /// value is less to leak.
    /// </para>
    /// </remarks>
    public static string? HashIp(string? ip, string key)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return null;
        }

        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(ip));
        return Convert.ToHexString(hash.AsSpan(0, 16)).ToLower(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A tombstone for a deleted address, so a returning person can be told
    /// "this was used before" without the address surviving.
    /// </summary>
    public static string EmailTombstone(string email, string key)
    {
        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(key),
            Encoding.UTF8.GetBytes(email.ToLowerInvariant()));

        return Convert.ToHexString(hash.AsSpan(0, 12)).ToLower(CultureInfo.InvariantCulture);
    }
}
