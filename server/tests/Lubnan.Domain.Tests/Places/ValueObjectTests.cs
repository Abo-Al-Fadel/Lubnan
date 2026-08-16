using Lubnan.Domain.Common;
using Lubnan.Domain.Places;
using Xunit;

namespace Lubnan.Domain.Tests.Places;

public sealed class SlugTests
{
    [Theory]
    [InlineData("byblos")]
    [InlineData("jeita-grotto")]
    [InlineData("beit-ed-dine-2")]
    public void Accepts_a_well_formed_slug(string value) =>
        Assert.True(Slug.Create(value).IsSuccess);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-byblos")]
    [InlineData("byblos-")]
    [InlineData("byblos--harbour")]
    [InlineData("byblos harbour")]
    [InlineData("byblos/harbour")]
    [InlineData("byblos?x=1")]
    [InlineData("جبيل")]
    public void Refuses_anything_that_would_need_escaping_in_a_url(string value) =>
        Assert.True(Slug.Create(value).IsFailure);

    [Fact]
    public void Normalises_case_and_surrounding_space()
    {
        // Two spellings of one URL is the failure mode this prevents: a link
        // shared as /explore/Byblos and one as /explore/byblos would otherwise
        // be two cache entries, two analytics rows and two canonical pages.
        var slug = Slug.Create("  Byblos  ");

        Assert.True(slug.IsSuccess);
        Assert.Equal("byblos", slug.Value.Value);
    }
}

public sealed class CoordinatesTests
{
    [Fact]
    public void Accepts_a_point_in_Lebanon() =>
        Assert.True(Coordinates.Create(34.12, 35.65).IsSuccess);

    [Fact]
    public void Refuses_latitude_and_longitude_the_wrong_way_round()
    {
        // Byblos with its arguments swapped lands in the Mediterranean off
        // Egypt. The bounds exist to catch exactly this, because the swapped
        // version is a perfectly valid pair of numbers.
        var result = Coordinates.Create(35.65, 34.12);

        Assert.True(result.IsFailure);
        Assert.Equal("coordinates.latitudeOutOfRange", result.Error.Code);
    }
}

public sealed class LocaleTests
{
    [Theory]
    [InlineData("en", "en")]
    [InlineData("FR", "fr")]
    [InlineData("ar-LB", "ar")]
    [InlineData("fr-CA,fr;q=0.9", "fr")]
    public void Parses_the_shapes_a_browser_actually_sends(string header, string expected)
    {
        Assert.True(Locale.TryParse(header, out var locale));
        Assert.Equal(expected, locale.Code);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("")]
    [InlineData(null)]
    public void Falls_back_to_English_for_anything_unpublished(string? header)
    {
        Assert.False(Locale.TryParse(header, out var locale));
        Assert.Equal(Locale.Default, locale);
    }

    [Fact]
    public void Arabic_is_the_only_right_to_left_locale()
    {
        Assert.True(Locale.Arabic.IsRightToLeft);
        Assert.All(Locale.All.Where(l => l != Locale.Arabic), l => Assert.False(l.IsRightToLeft));
    }
}

public sealed class ResultTests
{
    [Fact]
    public void A_success_carrying_an_error_cannot_be_constructed() =>
        Assert.Throws<InvalidOperationException>(() => Result.Failure(Error.None));

    [Fact]
    public void Reading_the_value_of_a_failure_throws() =>
        Assert.Throws<InvalidOperationException>(
            () => Result.NotFound<string>("x.missing", "gone").Value);

    [Fact]
    public void Map_carries_a_failure_through_untouched()
    {
        var mapped = Result.NotFound<int>("x.missing", "gone").Map(n => n.ToString(null as IFormatProvider));

        Assert.True(mapped.IsFailure);
        Assert.Equal("x.missing", mapped.Error.Code);
    }
}
