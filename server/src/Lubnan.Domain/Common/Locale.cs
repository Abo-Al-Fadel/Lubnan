namespace Lubnan.Domain.Common;

/// <summary>
/// A language the site publishes in. Validates on construction, so a locale
/// string that reached a query cannot be a locale string that reaches storage.
/// </summary>
/// <remarks>
/// Deliberately a closed set rather than any BCP 47 tag. Every locale here
/// costs an editorial obligation and, for search, its own index — so adding
/// one should be a decision somebody makes, not something a query string can
/// do. <see cref="TryParse"/> is the only way in.
/// </remarks>
public sealed class Locale : ValueObject
{
    public static readonly Locale English = new("en");
    public static readonly Locale French = new("fr");
    public static readonly Locale Arabic = new("ar");

    /// <summary>What a request without a usable Accept-Language gets.</summary>
    public static readonly Locale Default = English;

    public static readonly IReadOnlyList<Locale> All = [English, French, Arabic];

    private Locale(string code) => Code = code;

    /// <summary>Two-letter lowercase code: <c>en</c>, <c>fr</c>, <c>ar</c>.</summary>
    public string Code { get; }

    /// <summary>Arabic is written right to left; the frontend needs to know.</summary>
    public bool IsRightToLeft => Code == "ar";

    public static bool TryParse(string? code, out Locale locale)
    {
        // Accept-Language sends things like "fr-CA,fr;q=0.9" — take the base
        // subtag and ignore case, because a client is allowed to send "FR".
        var head = code?.Split(',', ';')[0].Split('-')[0].Trim().ToLowerInvariant();

        locale = All.FirstOrDefault(l => l.Code == head) ?? Default;
        return head is not null && locale.Code == head;
    }

    /// <summary>Falls back to <see cref="Default"/> rather than failing.</summary>
    public static Locale ParseOrDefault(string? code)
    {
        // The bool is genuinely uninteresting here: TryParse assigns the
        // default on failure, which is exactly what this method promises.
        _ = TryParse(code, out var locale);
        return locale;
    }

    protected override IEnumerable<object?> GetEqualityComponents() => [Code];

    public override string ToString() => Code;
}
