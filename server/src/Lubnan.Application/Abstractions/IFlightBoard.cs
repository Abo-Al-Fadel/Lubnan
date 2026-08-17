using Lubnan.Application.Features.Flights;

namespace Lubnan.Application.Abstractions;

/// <summary>
/// Today's arrivals and departures at Beirut–Rafic Hariri.
/// </summary>
/// <remarks>
/// The implementation talks to the airport. This interface does not, so a
/// handler can stay a query and the HTML scrape can be replaced without
/// touching the slice.
/// </remarks>
public interface IFlightBoard
{
    Task<FlightBoardDto> GetAsync(CancellationToken cancellationToken);
}
