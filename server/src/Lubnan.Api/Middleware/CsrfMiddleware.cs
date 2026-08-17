using System.Security.Cryptography;
using System.Text;
using Lubnan.Application.Abstractions.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lubnan.Api.Middleware;

/// <summary>
/// Double-submit CSRF, applied to every state-changing request.
/// </summary>
/// <remarks>
/// The problem: cookies are attached by the browser to any request to this
/// origin, including one caused by a form on a different site. So a page
/// anywhere on the internet can make an authenticated POST here on a reader's
/// behalf, without ever seeing the cookie.
/// <para>
/// The defence has two independent halves, and they are independent on purpose.
/// <c>SameSite=Lax</c> stops the browser sending the session cookie on a
/// cross-site POST at all — but it is enforced by the browser, and older or
/// unusual ones do not. The double-submit check is enforced here: a token is
/// issued in a cookie that <em>is</em> readable by script, and must be echoed
/// back in a header. Only a script on our own origin can read it; a cross-site
/// form can cause the request but cannot set a header.
/// </para>
/// <para>
/// Compared in constant time. A byte-by-byte comparison that returns early
/// leaks, through timing, how much of a guess was right — which is enough to
/// recover the token one character at a time.
/// </para>
/// </remarks>
internal sealed class CsrfMiddleware(RequestDelegate next)
{
    // The methods that cannot change anything, per RFC 9110. GET and HEAD are
    // also the ones a cross-site form cannot usefully forge anyway.
    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (SafeMethods.Contains(context.Request.Method) || !CarriesSession(context))
        {
            // No session cookie means nothing to abuse: an unauthenticated POST
            // is not a cross-site request forgery, because there is no identity
            // to borrow.
            await next(context).ConfigureAwait(false);
            return;
        }

        var fromCookie = context.Request.Cookies[AuthCookies.CsrfCookie];
        var fromHeader = context.Request.Headers[AuthCookies.CsrfHeader].ToString();

        if (string.IsNullOrEmpty(fromCookie) || string.IsNullOrEmpty(fromHeader) || !Matches(fromCookie, fromHeader))
        {
            await Reject(context).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool CarriesSession(HttpContext context) =>
        context.Request.Cookies.ContainsKey(AuthCookies.AccessCookie)
        || context.Request.Cookies.ContainsKey(AuthCookies.RefreshCookie);

    private static bool Matches(string a, string b)
    {
        // Hash first, then compare. FixedTimeEquals throws when the two
        // buffers differ in length, which would turn a forged token of the
        // wrong size into a 500 instead of a 403 — and the exception path is
        // a timing oracle for "this guess was the right length".
        var left = SHA256.HashData(Encoding.UTF8.GetBytes(a));
        var right = SHA256.HashData(Encoding.UTF8.GetBytes(b));
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static async Task Reject(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://lubnan.app/errors/forbidden",
            Title = "This request could not be verified. Reload the page and try again.",
            Status = StatusCodes.Status403Forbidden,
            Extensions = { ["code"] = "request.csrf" },
        }).ConfigureAwait(false);
    }
}
