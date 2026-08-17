using System.Reflection;
using System.Text.Json;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Security;
using Lubnan.Domain.Common;
using Lubnan.Domain.Community;
using Lubnan.Domain.Places;
using Lubnan.Domain.Users;
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
public sealed class DatabaseSeeder(
    AppDbContext db,
    IClock clock,
    IPasswordHasher passwords,
    ILogger<DatabaseSeeder> logger)
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

        var community = await SeedCommunityAsync(cancellationToken).ConfigureAwait(false);

        logger.Seeded(added, seeds.Count - added, community);
        return added + community;
    }

    private async Task<int> SeedCommunityAsync(CancellationToken cancellationToken)
    {
        if (await db.CommunityPosts.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return 0;
        }

        var now = clock.UtcNow;
        var hash = passwords.Hash($"seed-{Guid.NewGuid():N}-not-a-login");
        var authors = new (string Email, string Name)[]
        {
            ("rania.k@seed.lubnan.invalid", "Rania K."),
            ("marc.h@seed.lubnan.invalid", "Marc H."),
            ("yara.s@seed.lubnan.invalid", "Yara S."),
            ("elias.n@seed.lubnan.invalid", "Elias N."),
        };

        var users = new List<User>(authors.Length);
        foreach (var (address, name) in authors)
        {
            var email = Email.Create(address).Value;
            var existing = await db.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                users.Add(existing);
                continue;
            }

            var user = User.Register(email, DisplayName.Create(name).Value, hash, now).Value;
            user.ConfirmEmail(now);
            db.Users.Add(user);
            users.Add(user);
        }

        var captions = new (string Slug, string Plate, string Body)[]
        {
            ("qadisha", "D1", "Took the long path down from Bsharri. Three hours, one monastery cut into the cliff, and a man who insisted I take his thermos."),
            ("batroun", "D2", "The lemonade stand everyone tells you about is real, and it is worth the queue."),
            ("tyre", "D3", "Hippodrome at seven in the morning, completely empty. Two thousand years of chariot racing and one stray cat."),
            ("beirut", "D4", "Sunset from the Corniche. Half the city is out walking and someone is always selling corn."),
            ("cedars", "Q5", "Snow on the road above Bsharri closed it for two days. Worth waiting for."),
            ("beirut", "Q6", "Mezze that arrived in nine plates when we ordered four. Standard."),
            ("baalbek", "Q7", "Baalbek at golden hour. The columns are twenty-two metres and photographs do not carry it."),
            ("batroun", "Q8", "Diving off the rocks at Batroun. Water was colder than it looks here."),
            ("baalbek", "Q9", "Vineyard lunch in the Bekaa. Long table, dappled light, nobody left before dark."),
            ("tyre", "Q10", "First light at Tyre harbour. The boats go out before the tourists arrive."),
            ("jeita", "Q11", "Jeita from the boat. No cameras allowed inside so this is the entrance."),
            ("byblos", "Q12", "Byblos harbour, same size it has been for three thousand years."),
        };

        var added = 0;
        for (var i = 0; i < captions.Length; i++)
        {
            var (slug, plate, body) = captions[i];
            var author = users[i % users.Count];
            var published = CommunityPost.Publish(
                author.Id, body, slug, plate, now.AddHours(-(1 + (i * 3 % 20))));
            if (published.IsFailure)
            {
                throw new InvalidOperationException($"Community seed {i} is invalid: {published.Error.Message}");
            }

            db.CommunityPosts.Add(published.Value);
            added++;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
        Message = "Seed complete: {Added} places added, {Skipped} already present, {Community} community posts.")]
    public static partial void Seeded(this ILogger logger, int added, int skipped, int community);
}
