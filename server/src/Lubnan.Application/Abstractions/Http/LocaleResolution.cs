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
    /// Whatever wins is echoed in <c>Content-Language</c> and added to
    /// <c>Vary</c>, so a cache cannot serve the French copy to the next reader
    /// asking for Arabic. That omission is the classic i18n caching bug and it
    /// only appears once there is a CDN in front.
    /// </para>
    /// </remarks>
    public static Locale ResolveLocale(this HttpRequest request, string? explicitLocale = null)
    {
        var resolved = Locale.TryParse(explicitLocale, out var fromQuery)
            ? fromQuery
            : ResolveFromHeader(request);

        var response = request.HttpContext.Response;
        response.Headers.ContentLanguage = resolved.Code;
        response.Headers.Append("Vary", "Accept-Language");

        return resolved;
    }

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
