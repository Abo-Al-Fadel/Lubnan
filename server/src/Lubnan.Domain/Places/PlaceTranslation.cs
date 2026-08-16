using Lubnan.Domain.Common;

namespace Lubnan.Domain.Places;

/// <summary>
/// One place, written in one language. A row per locale, not a JSON blob.
/// </summary>
/// <remarks>
/// This is a table rather than a <c>jsonb</c> column on <see cref="Place"/>,
/// and the reason is worth stating because the blob looks cheaper:
/// <list type="bullet">
///   <item>Search needs a per-locale index with a per-locale stemmer. A blob
///   forces one index over all languages, which stems Arabic as English.</item>
///   <item>"Which places are missing Arabic" is a query here and a full scan
///   with JSON path predicates there.</item>
///   <item>A translation can be published on its own schedule. A blob has one
///   row version, so saving the French draft touches the English row.</item>
///   <item>Length and non-emptiness are check constraints here. In a blob they
///   are hopes.</item>
/// </list>
/// The blob wins on "add a fourth locale without a migration", which is a
/// thing that happens roughly never and takes ten minutes when it does.
/// </remarks>
public sealed class PlaceTranslation : Entity
{
    public const int MaxNameLength = 120;
    public const int MaxStandfirstLength = 400;

    private PlaceTranslation(
        Guid id,
        Guid placeId,
        Locale locale,
        string name,
        string? localName,
        string note,
        string standfirst,
        string body) : base(id)
    {
        PlaceId = placeId;
        Locale = locale;
        Name = name;
        LocalName = localName;
        Note = note;
        Standfirst = standfirst;
        Body = body;
    }

    private PlaceTranslation() { }

    public Guid PlaceId { get; private init; }

    public Locale Locale { get; private set; } = Locale.Default;

    /// <summary>The name as this language writes it: Byblos, Byblos, جبيل.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>What people there call it, when that differs: Jbeil.</summary>
    public string? LocalName { get; private set; }

    /// <summary>One sentence for the card in the mosaic.</summary>
    public string Note { get; private set; } = string.Empty;

    /// <summary>The standfirst under the banner.</summary>
    public string Standfirst { get; private set; } = string.Empty;

    /// <summary>The article itself.</summary>
    public string Body { get; private set; } = string.Empty;

    internal static Result<PlaceTranslation> Create(
        Guid placeId,
        Locale locale,
        string name,
        string? localName,
        string note,
        string standfirst,
        string body)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
        {
            return Result.Failure<PlaceTranslation>(Error.Validation(
                "translation.name",
                $"A name is required and may be at most {MaxNameLength} characters."));
        }

        if (standfirst.Length > MaxStandfirstLength)
        {
            return Result.Failure<PlaceTranslation>(Error.Validation(
                "translation.standfirst",
                $"A standfirst may be at most {MaxStandfirstLength} characters."));
        }

        return Result.Success(new PlaceTranslation(
            Guid.NewGuid(), placeId, locale, name.Trim(), localName?.Trim(), note, standfirst, body));
    }

    internal void Revise(string name, string? localName, string note, string standfirst, string body)
    {
        Name = name.Trim();
        LocalName = localName?.Trim();
        Note = note;
        Standfirst = standfirst;
        Body = body;
    }
}
