using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Security;
using Lubnan.Domain.Users;
using Microsoft.AspNetCore.Http;

namespace Lubnan.Application.Features.Identity;

/// <summary>
/// Everything a freshly-issued session needs, before any of it is a cookie.
/// </summary>
/// <remarks>
/// Handlers return this and endpoints turn it into <c>Set-Cookie</c>. The split
/// is what keeps a handler callable from a test with no HTTP context, and it is
/// enforced by the architecture test that forbids handlers from depending on
/// <c>Microsoft.AspNetCore</c>.
/// </remarks>
public sealed record SessionGrant(
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt,
    string CsrfToken);

/// <summary>Shared by the two slices that start a session.</summary>
internal static class SessionIssuer
{
    /// <summary>
    /// Mint an access token, a refresh token and a CSRF token, and attach the
    /// refresh side to a session row.
    /// </summary>
    public static SessionGrant Issue(
        User user,
        ITokenFactory tokens,
        AuthOptions options,
        DateTimeOffset now,
        string? userAgent,
        string? ipHash,
        UserSession? rotating = null)
    {
        var refresh = tokens.CreateRefreshToken();

        // Rotating an existing session keeps its family, so a stolen token
        // still traces back to the sign-in it came from. A new sign-in starts
        // a family of its own.
        if (rotating is not null)
        {
            user.Rotate(rotating, refresh.Hash, now, options.RefreshTokenLifetime, userAgent, ipHash);
        }
        else
        {
            user.StartSession(refresh.Hash, now, options.RefreshTokenLifetime, userAgent, ipHash);
        }

        var (accessToken, accessExpiresAt) = tokens.CreateAccessToken(user);

        // A fresh CSRF token per issue. It is not a secret in the way the
        // others are - it only has to be unguessable by a different origin -
        // but rotating it costs nothing and means a leaked one expires.
        var csrf = tokens.CreatePurposeToken().Value;

        return new SessionGrant(
            accessToken,
            accessExpiresAt,
            refresh.Value,
            now + options.RefreshTokenLifetime,
            csrf);
    }
}

/// <summary>Turning a grant into cookies, in one place.</summary>
public static class SessionGrantExtensions
{
    public static void Write(this SessionGrant grant, HttpResponse response, AuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(grant);

        AuthCookies.IssueSession(
            response,
            options,
            grant.AccessToken,
            grant.AccessExpiresAt,
            grant.RefreshToken,
            grant.RefreshExpiresAt,
            grant.CsrfToken);
    }
}
