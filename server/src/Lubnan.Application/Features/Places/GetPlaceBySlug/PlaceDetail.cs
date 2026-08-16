namespace Lubnan.Application.Features.Places.GetPlaceBySlug;

/// <summary>
/// The place page's whole payload: the article, the annotated callouts and the
/// practical strip, already resolved to one language.
/// </summary>
/// <remarks>
/// Resolved server-side rather than returning all three languages and letting
/// the client choose. The client asked for one; sending three would treble the
/// bytes on a page that is mostly prose, and it would put the fallback rule in
/// two places where the two could disagree.
/// </remarks>
/// <param name="Locale">
/// What was actually served, which is not always what was asked for — a place
/// with no Arabic copy answers an Arabic request in English. Saying so lets the
/// client mark the page as untranslated instead of silently misattributing it.
/// </param>
public sealed record PlaceDetail(
    string Slug,
    string Locale,
    string Name,
    string? LocalName,
    string Note,
    string Standfirst,
    string Body,
    string Region,
    string Category,
    string Index,
    double Latitude,
    double Longitude,
    PlateIds Plates,
    IReadOnlyList<CalloutView> Callouts,
    IReadOnlyList<FactView> Practical);

/// <summary>Plate ids for the page. Any may be absent; the frontend copes.</summary>
public sealed record PlateIds(string? Hero, string? Frame, string? Subject, string? Rail, string? Mosaic);

/// <param name="X">Fraction across the frame, 0 to 1, so it survives any crop.</param>
/// <param name="Y">Fraction down the frame, 0 to 1.</param>
public sealed record CalloutView(double X, double Y, string Label, string Body);

public sealed record FactView(string Label, string Value);
