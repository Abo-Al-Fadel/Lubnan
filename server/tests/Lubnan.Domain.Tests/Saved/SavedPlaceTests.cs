using Lubnan.Domain.Saved;
using Xunit;

namespace Lubnan.Domain.Tests.Saved;

public sealed class SavedPlaceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid User = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Pin_keeps_a_well_formed_slug()
    {
        var saved = SavedPlace.Pin(User, "Byblos", Now);

        Assert.True(saved.IsSuccess);
        Assert.Equal("byblos", saved.Value.PlaceSlug);
        Assert.Equal(User, saved.Value.UserId);
    }

    [Fact]
    public void Pin_refuses_an_empty_slug()
    {
        var saved = SavedPlace.Pin(User, "  ", Now);

        Assert.True(saved.IsFailure);
        Assert.Equal("slug.empty", saved.Error.Code);
    }

    [Fact]
    public void Pin_refuses_an_anonymous_account()
    {
        var saved = SavedPlace.Pin(Guid.Empty, "byblos", Now);

        Assert.True(saved.IsFailure);
        Assert.Equal("auth.required", saved.Error.Code);
    }
}
