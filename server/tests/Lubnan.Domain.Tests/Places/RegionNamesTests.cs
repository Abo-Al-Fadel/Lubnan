using Lubnan.Domain.Places;
using Xunit;

namespace Lubnan.Domain.Tests.Places;

public sealed class RegionNamesTests
{
    [Theory]
    [InlineData("MountLebanon")]
    [InlineData("mountlebanon")]
    [InlineData("Mount Lebanon")]
    [InlineData("mount-lebanon")]
    public void Parses_the_labels_the_site_actually_sends(string value)
    {
        Assert.True(RegionNames.TryParse(value, out var region));
        Assert.Equal(Region.MountLebanon, region);
    }

    [Fact]
    public void Rejects_an_unknown_band()
    {
        Assert.False(RegionNames.TryParse("Atlantis", out _));
    }
}
