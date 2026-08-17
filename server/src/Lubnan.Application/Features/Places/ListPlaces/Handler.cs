using System.Globalization;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Lubnan.Domain.Places;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.Places.ListPlaces;

internal sealed class Handler(IAppDbContext db)
    : IQueryHandler<Query, Result<IReadOnlyList<PlaceSummary>>>
{
    public async Task<Result<IReadOnlyList<PlaceSummary>>> Handle(
        Query query,
        CancellationToken cancellationToken)
    {
        // Two locales, not all of them: the one asked for and the one it falls
        // back to. Fetching every translation would treble the rows read in
        // order to discard two thirds of them.
        var requested = query.Locale;
        var fallback = Locale.Default;

        // Safe by the time the handler runs: the validator has already refused
        // anything these would not parse.
        Region? region = query.Region is null || !RegionNames.TryParse(query.Region, out var parsedRegion)
            ? null
            : parsedRegion;
        PlaceCategory? category = query.Category is null
            ? null
            : Enum.Parse<PlaceCategory>(query.Category, ignoreCase: true);

        var rows = await db.Places
            .AsNoTracking()
            .Where(p => p.PublishedAt != null)
            .Where(p => region == null || p.Region == region)
            .Where(p => category == null || p.Category == category)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new
            {
                p.Slug,
                p.Region,
                p.Category,
                p.DisplayOrder,
                p.Coordinates,
                p.Plates,
                Copy = p.Translations
                    .Where(t => t.Locale == requested || t.Locale == fallback)
                    .Select(t => new { t.Locale, t.Name, t.LocalName, t.Note })
                    .ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = rows.Select(row =>
        {
            var copy = row.Copy.FirstOrDefault(c => c.Locale == query.Locale)
                       ?? row.Copy.FirstOrDefault(c => c.Locale == Locale.Default);

            return new PlaceSummary(
                Slug: row.Slug.Value,
                Name: copy?.Name ?? row.Slug.Value,
                LocalName: copy?.LocalName,
                Note: copy?.Note ?? string.Empty,
                Region: row.Region.ToString(),
                Category: row.Category.ToString(),
                Index: (row.DisplayOrder + 1).ToString("00", CultureInfo.InvariantCulture),
                Latitude: row.Coordinates.Latitude,
                Longitude: row.Coordinates.Longitude,
                Plates: new PlateIds(
                    row.Plates.Hero,
                    row.Plates.Frame,
                    row.Plates.Subject,
                    row.Plates.Rail,
                    row.Plates.Mosaic));
        }).ToList();

        return Result.Success<IReadOnlyList<PlaceSummary>>(summaries);
    }
}
