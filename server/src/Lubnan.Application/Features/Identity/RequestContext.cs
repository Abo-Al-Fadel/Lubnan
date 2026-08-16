using Microsoft.AspNetCore.Http;

namespace Lubnan.Application.Features.Identity;

/// <summary>
/// The two things about a request that a session row records.
/// </summary>
/// <remarks>
/// Passed into commands rather than read inside handlers, so a handler can be
/// tested at a chosen address and user agent without a host — the same reason
/// the clock is injected.
/// </remarks>
public sealed record RequestFingerprint(string? UserAgent, string? Ip);

public static class RequestFingerprintExtensions
{
    public static RequestFingerprint Fingerprint(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RequestFingerprint(
            request.Headers.UserAgent.ToString(),

            // The connection address, not X-Forwarded-For. That header is set
            // by the client and only becomes trustworthy once a proxy is
            // configured to overwrite it — which is what ForwardedHeaders in
            // Program.cs does, and only for proxies we name. Reading the raw
            // header here would let anyone claim any address, including one
            // that makes their sign-in look like somebody else's.
            request.HttpContext.Connection.RemoteIpAddress?.ToString());
    }
}

/// <summary>Hashes an address for storage. Never stores the address.</summary>
public interface IIpHasher
{
    string? Hash(string? ip);
}

/// <summary>Builds the tombstone that outlives a deleted address.</summary>
public interface IEmailTombstoner
{
    string Tombstone(string email);
}
