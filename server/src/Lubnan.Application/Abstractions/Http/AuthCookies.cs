using Lubnan.Application.Abstractions.Security;
using Microsoft.AspNetCore.Http;

namespace Lubnan.Application.Abstractions.Http;

/// <summary>
/// Writes and clears the three cookies a session runs on.
/// </summary>
/// <remarks>
/// Cookies rather than an <c>Authorization</c> header, and the reason is the
/// one that decides most token designs: a value JavaScript can read is a value
/// an XSS payload can read. Storing an access token in <c>localStorage</c> or
/// in a variable means one compromised dependency — out of the hundreds in a
/// frontend's tree — exfiltrates every signed-in session. An httpOnly cookie is
/// attached by the browser and never visible to script, so the same compromise
/// can make requests as the user but cannot walk away with the credential.
/// <para>
/// The trade is CSRF: cookies are sent automatically, including on requests a
/// different site caused. That is what <see cref="CsrfCookie"/> and the
/// <c>SameSite</c> attribute are for, and why both are here rather than left to
/// each endpoint.
/// </para>
/// </remarks>
public static class AuthCookies
{
    public const string AccessCookie = "lubnan_at";
    public const string RefreshCookie = "lubnan_rt";
    public const string CsrfCookie = "lubnan_csrf";
    public const string CsrfHeader = "X-CSRF-Token";

    /// <summary>
    /// The refresh cookie is scoped to the auth routes and no further.
    /// </summary>
    /// <remarks>
    /// Every ordinary request would otherwise carry the long-lived credential
    /// as well as the short-lived one, so any log, proxy or crash dump that
    /// captured a single request would capture the token that mints all the
    /// others. Scoped, it travels only to the three endpoints that need it.
    /// </remarks>
    public const string RefreshPath = "/api/v1/auth";

    public static void IssueSession(
        HttpResponse response,
        AuthOptions options,
        string accessToken,
        DateTimeOffset accessExpiresAt,
        string refreshToken,
        DateTimeOffset refreshExpiresAt,
        string csrfToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(options);

        response.Cookies.Append(AccessCookie, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = options.RequireSecureCookies,

            // Lax, not Strict. Strict is not sent on a request that originated
            // from another site at all - including somebody following a link to
            // this one - so a shared link would land every reader on a page
            // that believes they are signed out. Lax withholds the cookie from
            // cross-site POSTs, which is the case that matters, and sends it on
            // ordinary navigation, which is the case that does not.
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = accessExpiresAt,
            IsEssential = true,
        });

        response.Cookies.Append(RefreshCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = options.RequireSecureCookies,
            SameSite = SameSiteMode.Lax,
            Path = RefreshPath,
            Expires = refreshExpiresAt,
            IsEssential = true,
        });

        // Deliberately readable by script. This is the double-submit half of
        // CSRF: the browser attaches the cookie automatically, but only a
        // script running on our own origin can read it and echo it back in the
        // header. A cross-site form can cause the request and cannot set the
        // header, so it fails.
        response.Cookies.Append(CsrfCookie, csrfToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = options.RequireSecureCookies,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = refreshExpiresAt,
            IsEssential = true,
        });
    }

    /// <summary>
    /// Remove all three. The attributes must match the ones they were set with
    /// or the browser deletes nothing and the user stays signed in.
    /// </summary>
    public static void ClearSession(HttpResponse response, AuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(options);

        var secure = options.RequireSecureCookies;

        response.Cookies.Delete(AccessCookie, new CookieOptions
        {
            HttpOnly = true, Secure = secure, SameSite = SameSiteMode.Lax, Path = "/",
        });

        response.Cookies.Delete(RefreshCookie, new CookieOptions
        {
            HttpOnly = true, Secure = secure, SameSite = SameSiteMode.Lax, Path = RefreshPath,
        });

        response.Cookies.Delete(CsrfCookie, new CookieOptions
        {
            HttpOnly = false, Secure = secure, SameSite = SameSiteMode.Lax, Path = "/",
        });
    }
}
