using Lubnan.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace Lubnan.Application.Abstractions.Http;

/// <summary>Decides which language a request is asking for.</summary>
public static class LocaleResolution
{
    /// <summary>
    /// An explicit <c>?locale=</c> wins; otherwise the first
    /// <c>Accept-Language</c> entry we publish in; otherwise English.
    /// </summary>
    /// <remarks>
    /// The query parameter has to override the header, because a reader whose
    /// browser is set to French may still want to read the Arabic — and a link
    /// they share must land the recipient on the language they were reading,
    /// not on the recipient's browser default.
    /// <para>
    /// <c>Vary: Accept-Language</c> is set here, so a cache cannot hand the
    /// French copy to the next reader asking for Arabic. That omission is the
    /// classic i18n caching bug and it only shows up once there is a CDN in
    /// front of the origin.
    /// </para>
    /// <para>
    /// <c>Content-Language</c> is deliberately <em>not</em> set here. It
    /// describes the language of the body, and this method runs before the
    /// handler — so it cannot know whether the copy actually exists. Setting it
    /// to the requested locale would mean answering an Arabic request with
    /// English prose labelled <c>ar</c>, which misleads caches, translation
    /// tooling and screen readers alike. Each endpoint sets it from what it
    /// really served; see <see cref="SetContentLanguage"/>.
    /// </para>
    /// </remarks>
    public static Locale ResolveLocale(this HttpRequest request, string? explicitLocale = null)
    {
        var resolved = Locale.TryParse(explicitLocale, out var fromQuery)
            ? fromQuery
            : ResolveFromHeader(request);

        request.HttpContext.Response.Headers.Append("Vary", "Accept-Language");

        return resolved;
    }

    /// <summary>
    /// Declares the language the body is actually written in, which is not
    /// always the one that was asked for.
    /// </summary>
    public static void SetContentLanguage(this HttpRequest request, string localeCode) =>
        request.HttpContext.Response.Headers.ContentLanguage = localeCode;

    private static Locale ResolveFromHeader(HttpRequest request)
    {
        // Ordered by q, highest first, so "ar;q=0.9, en;q=0.5" gets Arabic.
        var candidates = request.Headers.AcceptLanguage
            .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(Parse)
            .OrderByDescending(entry => entry.Quality);

        foreach (var (tag, _) in candidates)
        {
            if (Locale.TryParse(tag, out var locale))
            {
                return locale;
            }
        }

        return Locale.Default;
    }

    private static (string Tag, double Quality) Parse(string entry)
    {
        var parts = entry.Split(';', StringSplitOptions.TrimEntries);
        var tag = parts[0];

        var quality = parts
            .Skip(1)
            .Where(p => p.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
            .Select(p => double.TryParse(p[2..], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var q) ? q : 1d)
            .DefaultIfEmpty(1d)
            .First();

        return (tag, quality);
    }
}
