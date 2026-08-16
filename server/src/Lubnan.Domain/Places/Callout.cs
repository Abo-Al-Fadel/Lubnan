using Lubnan.Domain.Common;

namespace Lubnan.Domain.Places;

/// <summary>The localised text of one callout.</summary>
public sealed record CalloutText(string Label, string Body);

/// <summary>
/// A labelled point on the place's photograph. The callouts are what stop the
/// banner being decorative: each names a real thing at a real position.
/// </summary>
/// <remarks>
/// Position is stored in columns and prose in <c>jsonb</c>, and that split is
/// the rule this schema follows everywhere: <b>a column for anything you
/// filter, sort, constrain or search on; JSON for prose that is only ever read
/// as part of its parent.</b>
/// <para>
/// So <see cref="PlaceTranslation"/> gets a table — its body is searched — and
/// callout labels do not, because nothing queries them. Keeping X and Y as
/// columns also means moving a dot is one write, not one per language.
/// </para>
/// </remarks>
public sealed class Callout : Entity
{
    private readonly Dictionary<string, CalloutText> _text = [];

    private Callout(Guid id, Guid placeId, int ordinal, double x, double y) : base(id)
    {
        PlaceId = placeId;
        Ordinal = ordinal;
        X = x;
        Y = y;
    }

    private Callout() { }

    public Guid PlaceId { get; private init; }

    /// <summary>Reading order, which is not the same as position on the plate.</summary>
    public int Ordinal { get; private set; }

    /// <summary>Fraction across the plate, 0 to 1, so it survives any crop.</summary>
    public double X { get; private set; }

    /// <summary>Fraction down the plate, 0 to 1.</summary>
    public double Y { get; private set; }

    public IReadOnlyDictionary<string, CalloutText> Text => _text;

    internal static Result<Callout> Create(Guid placeId, int ordinal, double x, double y)
    {
        // Fractions, not pixels. A value outside the frame means somebody
        // pasted pixel coordinates, which would put the dot off-plate at every
        // size and is worth catching here rather than in a screenshot.
        if (x is < 0 or > 1 || y is < 0 or > 1)
        {
            return Result.Failure<Callout>(Error.Validation(
                "callout.outOfFrame",
                "Callout coordinates are fractions of the plate, between 0 and 1."));
        }

        return Result.Success(new Callout(Guid.NewGuid(), placeId, ordinal, x, y));
    }

    internal void SetText(Locale locale, CalloutText text) => _text[locale.Code] = text;

    /// <summary>
    /// The requested language, falling back to English rather than to nothing.
    /// A callout with no text at all would render as a bare dot.
    /// </summary>
    public CalloutText? In(Locale locale) =>
        _text.TryGetValue(locale.Code, out var text) ? text
        : _text.TryGetValue(Locale.Default.Code, out var fallback) ? fallback
        : null;
}
