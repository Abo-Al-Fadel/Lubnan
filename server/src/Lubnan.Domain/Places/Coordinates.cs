using Lubnan.Domain.Common;

namespace Lubnan.Domain.Places;

/// <summary>A point on the earth, in WGS 84.</summary>
/// <remarks>
/// Latitude first in conversation, longitude first in GeoJSON, and the two get
/// swapped constantly. Naming both parameters and refusing anything outside
/// Lebanon's bounding box catches the swap at construction instead of when a
/// destination renders in the Indian Ocean.
/// <para>
/// No NetTopologySuite type here on purpose: <c>Point</c> belongs to a
/// persistence library, and the domain does not take a dependency to describe
/// a pair of numbers. Infrastructure converts at the boundary.
/// </para>
/// </remarks>
public sealed class Coordinates : ValueObject
{
    // Generous bounds around Lebanon: roughly Naqoura to Arida, coast to the
    // Anti-Lebanon ridge, with margin for a site just over a border.
    private const double MinLatitude = 32.9;
    private const double MaxLatitude = 34.8;
    private const double MinLongitude = 34.9;
    private const double MaxLongitude = 36.7;

    private Coordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>EF materialises through this; nothing else should.</summary>
    private Coordinates() { }

    public double Latitude { get; }

    public double Longitude { get; }

    public static Result<Coordinates> Create(double latitude, double longitude)
    {
        if (latitude is < MinLatitude or > MaxLatitude)
        {
            return Result.Failure<Coordinates>(Error.Validation(
                "coordinates.latitudeOutOfRange",
                $"Latitude {latitude} is outside Lebanon ({MinLatitude} to {MaxLatitude}). Are latitude and longitude the right way round?"));
        }

        if (longitude is < MinLongitude or > MaxLongitude)
        {
            return Result.Failure<Coordinates>(Error.Validation(
                "coordinates.longitudeOutOfRange",
                $"Longitude {longitude} is outside Lebanon ({MinLongitude} to {MaxLongitude}). Are latitude and longitude the right way round?"));
        }

        return Result.Success(new Coordinates(latitude, longitude));
    }

    protected override IEnumerable<object?> GetEqualityComponents() => [Latitude, Longitude];
}
