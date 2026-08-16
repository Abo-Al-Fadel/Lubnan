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

/// <summary>What kind of place it is; drives the filter chips and the icon.</summary>
public enum PlaceCategory
{
    Ruins,
    Nature,
    Mountains,
    Coast,
    City,
}
