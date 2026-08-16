using Lubnan.Domain.Common;
using Lubnan.Domain.Places.Events;

namespace Lubnan.Domain.Places;

/// <summary>
/// A destination: Byblos, Baalbek, the Qadisha valley. The aggregate root for
/// everything the place page renders.
/// </summary>
/// <remarks>
/// Translations, callouts and practical facts live inside this boundary and
/// are only reachable through it. That is what makes rules like "a place
/// cannot be published without English copy" enforceable in one place instead
/// of at the top of every handler that touches a place.
/// <para>
/// Nothing outside adds a translation by constructing one; they call
/// <see cref="Translate"/>. The child constructors are <c>internal</c> and the
/// collections are exposed read-only, so the compiler enforces that rather
/// than a code review.
/// </para>
/// </remarks>
public sealed class Place : AggregateRoot
{
    private readonly List<PlaceTranslation> _translations = [];
    private readonly List<Callout> _callouts = [];

    // Named for the navigation rather than for brevity. EF finds a backing
    // field by convention from the property name, and "_facts" behind
    // "PracticalFacts" fails that lookup at model-build time.
    private readonly List<PracticalFact> _practicalFacts = [];

    private Place(Guid id, Slug slug, Region region, PlaceCategory category, Coordinates coordinates)
        : base(id)
    {
        Slug = slug;
        Region = region;
        Category = category;
        Coordinates = coordinates;
        Plates = PlateSet.Empty;
    }

    private Place() { }

    public Slug Slug { get; private set; } = null!;

    public Region Region { get; private set; }

    public PlaceCategory Category { get; private set; }

    public Coordinates Coordinates { get; private set; } = null!;

    public PlateSet Plates { get; private set; } = PlateSet.Empty;

    /// <summary>
    /// Editorial order in the mosaic. Not the id and not alphabetical: the
    /// sequence somebody chose, which the frontend shows as "01", "02".
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Null until published. The public API filters on this, never on a flag.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    public bool IsPublished => PublishedAt is not null;

    public IReadOnlyList<PlaceTranslation> Translations => _translations.AsReadOnly();

    public IReadOnlyList<Callout> Callouts => _callouts.AsReadOnly();

    public IReadOnlyList<PracticalFact> PracticalFacts => _practicalFacts.AsReadOnly();

    /// <summary>
    /// Returns a <see cref="Place"/> rather than a <c>Result</c>, and that is
    /// the point of value objects: every argument has already been validated by
    /// its own type, so there is no failure left for this method to report.
    /// </summary>
    public static Place Create(
        Slug slug,
        Region region,
        PlaceCategory category,
        Coordinates coordinates,
        int displayOrder) =>
        new(Guid.NewGuid(), slug, region, category, coordinates) { DisplayOrder = displayOrder };

    public void SetPlates(PlateSet plates) => Plates = plates;

    public void MoveTo(Coordinates coordinates) => Coordinates = coordinates;

    /// <summary>
    /// Add or revise the copy for one language. Idempotent by locale, so a
    /// re-run of the seeder edits rather than duplicates.
    /// </summary>
    public Result<PlaceTranslation> Translate(
        Locale locale,
        string name,
        string? localName,
        string note,
        string standfirst,
        string body)
    {
        var existing = _translations.FirstOrDefault(t => t.Locale == locale);

        if (existing is not null)
        {
            existing.Revise(name, localName, note, standfirst, body);
            Raise(new PlaceTranslationRevised(Id, Slug.Value, locale.Code));
            return Result.Success(existing);
        }

        var created = PlaceTranslation.Create(Id, locale, name, localName, note, standfirst, body);
        if (created.IsFailure)
        {
            return created;
        }

        _translations.Add(created.Value);
        return created;
    }

    /// <summary>
    /// Place a callout on the plate, with its text in one or more languages.
    /// </summary>
    /// <remarks>
    /// The text comes in here rather than being set on the returned callout,
    /// so every change to the aggregate goes through the root. A caller holding
    /// a <see cref="Callout"/> can read it and nothing else, which is what makes
    /// the boundary real rather than a naming convention.
    /// <para>
    /// The ordinal is assigned here too, because a caller choosing its own would
    /// eventually choose a duplicate and the strip would render in an order
    /// nobody picked.
    /// </para>
    /// </remarks>
    public Result<Callout> AddCallout(double x, double y, IEnumerable<KeyValuePair<Locale, CalloutText>> text)
    {
        var created = Callout.Create(Id, _callouts.Count, x, y);
        if (created.IsFailure)
        {
            return created;
        }

        foreach (var (locale, value) in text)
        {
            created.Value.SetText(locale, value);
        }

        _callouts.Add(created.Value);
        return created;
    }

    public PracticalFact AddFact(IEnumerable<KeyValuePair<Locale, FactText>> text)
    {
        var fact = PracticalFact.Create(Id, _practicalFacts.Count);

        foreach (var (locale, value) in text)
        {
            fact.SetText(locale, value);
        }

        _practicalFacts.Add(fact);
        return fact;
    }

    /// <summary>
    /// Make it public. The invariant that matters lives here: a place with no
    /// copy in the default locale would render as an empty page in the fallback
    /// path, and no amount of frontend defensiveness fixes an empty article.
    /// </summary>
    public Result Publish(DateTimeOffset now)
    {
        if (IsPublished)
        {
            return Result.Success();
        }

        if (!_translations.Any(t => t.Locale == Locale.Default))
        {
            return Result.Failure(Error.Validation(
                "place.noDefaultTranslation",
                $"A place cannot be published without copy in {Locale.Default.Code}, which is what every other locale falls back to."));
        }

        PublishedAt = now;
        Raise(new PlacePublished(Id, Slug.Value));
        return Result.Success();
    }

    public Result Unpublish()
    {
        if (!IsPublished)
        {
            return Result.Success();
        }

        PublishedAt = null;
        Raise(new PlaceUnpublished(Id, Slug.Value));
        return Result.Success();
    }

    /// <summary>
    /// The copy for a language, falling back to the default rather than to
    /// null. Callers render a page; they should not each invent a fallback.
    /// </summary>
    public PlaceTranslation? Copy(Locale locale) =>
        _translations.FirstOrDefault(t => t.Locale == locale)
        ?? _translations.FirstOrDefault(t => t.Locale == Locale.Default);
}
