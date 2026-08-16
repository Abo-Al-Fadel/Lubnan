namespace Lubnan.Api.IntegrationTests;

/// <summary>
/// The response shapes, declared here rather than referenced from the
/// Application assembly.
/// </summary>
/// <remarks>
/// Deliberate duplication. These tests are a client, and a client that shares
/// types with the server cannot detect a breaking change — rename a property on
/// both sides at once and every test still passes while every real consumer
/// breaks. Written out separately, the JSON contract is what is being asserted.
/// </remarks>
public sealed record PlaceSummaryDto(
    string Slug,
    string Name,
    string? LocalName,
    string Note,
    string Region,
    string Category,
    string Index,
    double Latitude,
    double Longitude,
    PlateIdsDto Plates);

public sealed record PlaceDetailDto(
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
    PlateIdsDto Plates,
    IReadOnlyList<CalloutDto> Callouts,
    IReadOnlyList<FactDto> Practical);

public sealed record PlateIdsDto(string? Hero, string? Frame, string? Subject, string? Rail, string? Mosaic);

public sealed record CalloutDto(double X, double Y, string Label, string Body);

public sealed record FactDto(string Label, string Value);

/// <summary>RFC 7807, plus the <c>code</c> extension this API adds.</summary>
public sealed record ProblemDto(
    string? Type,
    string? Title,
    int? Status,
    string? Code,
    Dictionary<string, string[]>? Errors);
