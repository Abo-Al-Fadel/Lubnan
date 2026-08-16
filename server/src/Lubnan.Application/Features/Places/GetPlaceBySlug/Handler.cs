using System.Globalization;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Lubnan.Domain.Places;
using Microsoft.EntityFrameworkCore;

namespace Lubnan.Application.Features.Places.GetPlaceBySlug;

internal sealed class Handler(IAppDbContext db) : IQueryHandler<Query, Result<PlaceDetail>>
{
    public async Task<Result<PlaceDetail>> Handle(Query query, CancellationToken cancellationToken)
    {
        var parsed = Slug.Create(query.Slug);
        if (parsed.IsFailure)
        {
            return Result.Failure<PlaceDetail>(parsed.Error);
        }

        var slug = parsed.Value;

        var place = await db.Places
            .AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Callouts)
            .Include(p => p.PracticalFacts)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.PublishedAt != null, cancellationToken)
            .ConfigureAwait(false);

        if (place is null)
        {
            // Unpublished and non-existent answer identically and on purpose.
            // A distinguishable 403 would let anyone enumerate the slugs of
            // work that has not been announced yet.
            return Result.NotFound<PlaceDetail>(
                "place.notFound", $"No published place is called '{query.Slug}'.");
        }

        var copy = place.Copy(query.Locale);
        if (copy is null)
        {
            // Publish() refuses to let this happen. If it has happened anyway,
            // the row was written around the domain and that is worth a 500
            // rather than an empty page that looks like a design decision.
            return Result.Failure<PlaceDetail>(Error.Failure(
                "place.noCopy", "That place has no editorial copy in any language."));
        }

        return Result.Success(new PlaceDetail(
            Slug: place.Slug.Value,
            Locale: copy.Locale.Code,
            Name: copy.Name,
            LocalName: copy.LocalName,
            Note: copy.Note,
            Standfirst: copy.Standfirst,
            Body: copy.Body,
            Region: place.Region.ToString(),
            Category: place.Category.ToString(),
            Index: (place.DisplayOrder + 1).ToString("00", CultureInfo.InvariantCulture),
            Latitude: place.Coordinates.Latitude,
            Longitude: place.Coordinates.Longitude,
            Plates: new PlateIds(
                place.Plates.Hero,
                place.Plates.Frame,
                place.Plates.Subject,
                place.Plates.Rail,
                place.Plates.Mosaic),
            Callouts: place.Callouts
                .OrderBy(c => c.Ordinal)
                .Select(c => (Callout: c, Text: c.In(query.Locale)))
                .Where(c => c.Text is not null)
                .Select(c => new CalloutView(c.Callout.X, c.Callout.Y, c.Text!.Label, c.Text.Body))
                .ToList(),
            Practical: place.PracticalFacts
                .OrderBy(f => f.Ordinal)
                .Select(f => f.In(query.Locale))
                .Where(t => t is not null)
                .Select(t => new FactView(t!.Label, t.Value))
                .ToList()));
    }
}
