namespace Lubnan.Api.Middleware;

/// <summary>
/// The headers a browser needs in order to defend a response it has been given.
/// </summary>
/// <remarks>
/// This is a JSON API, so several of these are belt-and-braces — but they are
/// cheap, and the assumption that "nobody renders our responses" fails the
/// first time a browser is pointed straight at an endpoint, or the day someone
/// adds an HTML error page.
/// </remarks>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = context.Response.Headers;

        // Stop the browser guessing a content type. Without it, a JSON response
        // whose body an attacker influenced can be sniffed as HTML and run as a
        // page on this origin, which is XSS via a response that was never meant
        // to be rendered.
        headers["X-Content-Type-Options"] = "nosniff";

        // An API returns nothing worth framing. frame-ancestors in the CSP is
        // what modern browsers honour; X-Frame-Options is for the old ones.
        headers["X-Frame-Options"] = "DENY";

        // Referer leaks URLs to third parties, and URLs here contain slugs,
        // ids and - on a confirmation link - tokens.
        headers["Referrer-Policy"] = "no-referrer";

        // Nothing served from here needs a camera, a microphone or a location.
        headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), payment=(), usb=()";

        // A locked-down CSP for responses that are not documents. default-src
        // 'none' means that if any of this is ever rendered, it can load
        // nothing, run nothing and connect nowhere. The frontend has its own,
        // much less restrictive policy; this one is for the API.
        headers["Content-Security-Policy"] =
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

        // Tell the world less about what is running. Version numbers in headers
        // are how a scanner decides which exploit to try first.
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        await next(context).ConfigureAwait(false);
    }
}
