namespace Lubnan.Domain.Places;

/// <summary>
/// The five bands the site groups destinations into. Not the administrative
/// governorates: this is the visitor's mental map, which is the one the region
/// picker on <c>/explore</c> is built around.
/// </summary>
/// <remarks>
/// Persisted by name, not by number. An enum stored as an integer means a
/// reordered member silently rewrites history in the database, and the values
/// become unreadable in any tool that is not this codebase.
/// </remarks>
public enum Region
{
    Coast,
    MountLebanon,
    North,
    South,
    Bekaa,
}

/// <summary>
/// Accepts both the enum name and the label the site shows: <c>MountLebanon</c>
/// and <c>Mount Lebanon</c> are the same region.
/// </summary>
public static class RegionNames
{
    public static bool TryParse(string? value, out Region region)
    {
        region = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var compact = value
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);

        return Enum.TryParse(compact, ignoreCase: true, out region);
    }
}

/// <summary>What kind of place it is; drives the filter chips and the icon.</summary>
public enum PlaceCategory
{
    Ruins,
    Nature,
    Mountains,
    Coast,
    City,
}
