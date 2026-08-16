using Lubnan.Domain.Common;

namespace Lubnan.Domain.Places;

/// <summary>One label and value from the practical strip, in one language.</summary>
public sealed record FactText(string Label, string Value);

/// <summary>
/// A row of the practical strip: getting there, best season, hours, entry.
/// </summary>
/// <remarks>
/// The labels are prose rather than a closed set — one place lists a snowline,
/// another a bakery worth queueing at — so this is not an enum with translated
/// captions. Same rule as <see cref="Callout"/>: ordinal in a column, prose in
/// <c>jsonb</c>, because nothing queries the text.
/// </remarks>
public sealed class PracticalFact : Entity
{
    private readonly Dictionary<string, FactText> _text = [];

    private PracticalFact(Guid id, Guid placeId, int ordinal) : base(id)
    {
        PlaceId = placeId;
        Ordinal = ordinal;
    }

    private PracticalFact() { }

    public Guid PlaceId { get; private init; }

    public int Ordinal { get; private set; }

    public IReadOnlyDictionary<string, FactText> Text => _text;

    internal static PracticalFact Create(Guid placeId, int ordinal) =>
        new(Guid.NewGuid(), placeId, ordinal);

    internal void SetText(Locale locale, FactText text) => _text[locale.Code] = text;

    public FactText? In(Locale locale) =>
        _text.TryGetValue(locale.Code, out var text) ? text
        : _text.TryGetValue(Locale.Default.Code, out var fallback) ? fallback
        : null;
}
