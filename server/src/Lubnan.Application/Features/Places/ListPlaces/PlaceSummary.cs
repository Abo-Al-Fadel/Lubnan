namespace Lubnan.Application.Features.Places.ListPlaces;

/// <summary>
/// One card. Deliberately smaller than the detail response: a list of forty
/// places should not carry forty article bodies because one of them might be
/// clicked.
/// </summary>
/// <param name="Slug">The identifier the client links to and refetches by.</param>
/// <param name="Index">Editorial position, zero-padded, as the design shows it.</param>
/// <param name="Plates">
/// Plate <em>ids</em>, not URLs. The frontend resolves those, because it is
/// the half that knows the viewport and the extension chain.
/// </param>
public sealed record PlaceSummary(
    string Slug,
    string Name,
    string? LocalName,
    string Note,
    string Region,
    string Category,
    string Index,
    double Latitude,
    double Longitude,
    PlateIds Plates);

/// <summary>The plate ids a card can use. Any of them may be absent.</summary>
public sealed record PlateIds(string? Hero, string? Frame, string? Subject, string? Rail, string? Mosaic);
