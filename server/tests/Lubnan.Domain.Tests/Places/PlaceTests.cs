using Lubnan.Domain.Common;
using Lubnan.Domain.Places;
using Lubnan.Domain.Places.Events;
using Xunit;

namespace Lubnan.Domain.Tests.Places;

/// <summary>
/// Rules the aggregate is supposed to enforce. No host, no database, no
/// substitutes — these run in milliseconds because the domain has no
/// dependencies to stand up.
/// </summary>
public sealed class PlaceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);

    private static Place ByblosDraft() => Place.Create(
        Slug.Create("byblos").Value,
        Region.Coast,
        PlaceCategory.Ruins,
        Coordinates.Create(34.12, 35.65).Value,
        displayOrder: 0);

    [Fact]
    public void Publishing_without_copy_in_the_fallback_language_is_refused()
    {
        var place = ByblosDraft();

        var result = place.Publish(Now);

        Assert.True(result.IsFailure);
        Assert.Equal("place.noDefaultTranslation", result.Error.Code);
        Assert.False(place.IsPublished);
    }

    [Fact]
    public void Publishing_with_only_a_non_default_translation_is_still_refused()
    {
        // Arabic alone is not enough: every other locale falls back to English,
        // so an English-less place renders as an empty article to two thirds of
        // the site's readers.
        var place = ByblosDraft();
        place.Translate(Locale.Arabic, "جبيل", null, "note", "standfirst", "body");

        Assert.True(place.Publish(Now).IsFailure);
    }

    [Fact]
    public void Publishing_raises_the_event_once()
    {
        var place = ByblosDraft();
        place.Translate(Locale.English, "Byblos", "Jbeil", "note", "standfirst", "body");

        Assert.True(place.Publish(Now).IsSuccess);
        Assert.Equal(Now, place.PublishedAt);

        var published = place.DomainEvents.OfType<PlacePublished>().ToList();
        Assert.Single(published);
        Assert.Equal("byblos", published[0].Slug);

        // Publishing twice is a no-op, not a second announcement. Consumers
        // rebuild sitemaps and warm caches off this; two events means twice the
        // work for a button pressed twice.
        Assert.True(place.Publish(Now.AddHours(1)).IsSuccess);
        Assert.Single(place.DomainEvents.OfType<PlacePublished>());
        Assert.Equal(Now, place.PublishedAt);
    }

    [Fact]
    public void Translating_the_same_locale_twice_revises_rather_than_duplicates()
    {
        var place = ByblosDraft();
        place.Translate(Locale.English, "Byblos", "Jbeil", "note", "standfirst", "first");
        place.Translate(Locale.English, "Byblos", "Jbeil", "note", "standfirst", "second");

        Assert.Single(place.Translations);
        Assert.Equal("second", place.Translations[0].Body);
    }

    [Fact]
    public void Copy_falls_back_to_the_default_locale()
    {
        var place = ByblosDraft();
        place.Translate(Locale.English, "Byblos", "Jbeil", "note", "standfirst", "body");

        var french = place.Copy(Locale.French);

        Assert.NotNull(french);
        Assert.Equal(Locale.English, french.Locale);
    }

    [Fact]
    public void Callout_ordinals_are_assigned_in_the_order_they_are_added()
    {
        var place = ByblosDraft();
        var text = new[] { KeyValuePair.Create(Locale.English, new CalloutText("Keep", "Twelfth century.")) };

        place.AddCallout(0.28, 0.42, text);
        place.AddCallout(0.62, 0.66, text);

        Assert.Equal([0, 1], place.Callouts.Select(c => c.Ordinal));
    }

    [Theory]
    [InlineData(-0.01, 0.5)]
    [InlineData(1.01, 0.5)]
    [InlineData(0.5, 1.5)]
    public void Callouts_outside_the_frame_are_refused(double x, double y)
    {
        // Pixel coordinates pasted where fractions were expected. Caught here
        // rather than in a screenshot review after the dot renders off-plate.
        var result = ByblosDraft().AddCallout(x, y, []);

        Assert.True(result.IsFailure);
        Assert.Equal("callout.outOfFrame", result.Error.Code);
    }
}
