using System.Reflection;
using System.Text.Json;
using Lubnan.Application.Abstractions;
using Lubnan.Domain.Common;
using Lubnan.Domain.Places;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lubnan.Infrastructure.Persistence.Seed;

/// <summary>
/// Puts the eight destinations into an empty database, going through the
/// domain rather than around it.
/// </summary>
/// <remarks>
/// Every row here is built by calling <c>Place.Create</c>, <c>Translate</c>,
/// <c>AddCallout</c> and <c>Publish</c> — the same path a future admin API will
/// take. So the seed cannot produce a place the domain would refuse, and the
/// invariants get exercised on every fresh database instead of only in tests.
/// <para>
/// Idempotent by slug: running it twice adds nothing and running it against a
/// half-seeded database completes it. That matters because it is the thing
/// somebody will run when they are not sure whether they already have.
/// </para>
/// <para>
/// Not a hosted service and not called from <c>Program.cs</c>. It runs when
/// asked: <c>dotnet run --project src/Lubnan.Api -- seed</c>. Startup seeding
/// races between replicas and, on the day it goes wrong, writes to production.
/// </para>
/// </remarks>
public sealed class DatabaseSeeder(AppDbContext db, IClock clock, ILogger<DatabaseSeeder> logger)
{
    private const string ResourceName = "Lubnan.Infrastructure.Persistence.Seed.places.seed.json";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        var seeds = Load();

        var existing = await db.Places
            .Select(p => p.Slug)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var known = existing.Select(s => s.Value).ToHashSet(StringComparer.Ordinal);
        var added = 0;

        foreach (var seed in seeds)
        {
            if (known.Contains(seed.Slug))
            {
                continue;
            }

            var place = Build(seed);
            db.Places.Add(place);
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.Seeded(added, seeds.Count - added);
        return added;
    }

    private Place Build(PlaceSeed seed)
    {
        var slug = Slug.Create(seed.Slug);
        var coordinates = Coordinates.Create(seed.Latitude, seed.Longitude);

        // The seed file is generated, so a failure here is a broken generator
        // rather than bad user input. Throwing names the offending row and
        // stops, which is what you want from a data-loading step.
        if (slug.IsFailure || coordinates.IsFailure)
        {
            throw new InvalidOperationException(
                $"Seed row '{seed.Slug}' is invalid: {(slug.IsFailure ? slug.Error : coordinates.Error).Message}");
        }

        var place = Place.Create(
            slug.Value,
            Enum.Parse<Region>(seed.Region),
            Enum.Parse<PlaceCategory>(seed.Category),
            coordinates.Value,
            seed.DisplayOrder);

        place.SetPlates(PlateSet.Create(
            seed.Plates.Hero, seed.Plates.Frame, seed.Plates.Subject, seed.Plates.Rail, seed.Plates.Mosaic));

        foreach (var (code, copy) in seed.Translations)
        {
            var translated = place.Translate(
                Locale.ParseOrDefault(code), copy.Name, copy.LocalName, copy.Note, copy.Standfirst, copy.Body);

            if (translated.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seed row '{seed.Slug}' [{code}] is invalid: {translated.Error.Message}");
            }
        }

        foreach (var calloutSeed in seed.Callouts)
        {
            var callout = place.AddCallout(
                calloutSeed.X,
                calloutSeed.Y,
                calloutSeed.Text.Select(entry => KeyValuePair.Create(
                    Locale.ParseOrDefault(entry.Key),
                    new CalloutText(entry.Value.Label, entry.Value.Body))));

            if (callout.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seed row '{seed.Slug}' has a callout outside the frame: {callout.Error.Message}");
            }
        }

        foreach (var factSeed in seed.Practical)
        {
            place.AddFact(factSeed.Text.Select(entry => KeyValuePair.Create(
                Locale.ParseOrDefault(entry.Key),
                new FactText(entry.Value.Label, entry.Value.Value))));
        }

        // Publish() is what enforces "no place goes public without copy in the
        // fallback language". Going through it means the seed cannot create a
        // row that the admin API would have rejected.
        var published = place.Publish(clock.UtcNow);
        if (published.IsFailure)
        {
            throw new InvalidOperationException($"Seed row '{seed.Slug}': {published.Error.Message}");
        }

        return place;
    }

    private static List<PlaceSeed> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"{ResourceName} is not embedded. Run: node server/scripts/export-seed.mjs");

        return JsonSerializer.Deserialize<List<PlaceSeed>>(stream, Options)
            ?? throw new InvalidOperationException($"{ResourceName} is empty.");
    }
}

internal static partial class DatabaseSeederMessages
{
    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message = "Seed complete: {Added} places added, {Skipped} already present.")]
    public static partial void Seeded(this ILogger logger, int added, int skipped);
}
