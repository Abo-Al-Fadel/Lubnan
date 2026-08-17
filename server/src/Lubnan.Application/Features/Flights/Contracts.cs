namespace Lubnan.Application.Features.Flights;

/// <summary>One row on the BEY board.</summary>
public sealed record FlightRowDto(
    string Code,
    string Airline,
    string? Iata,
    string City,
    string Country,
    string Time,
    int Delay,
    string Status,
    string Terminal,
    string Gate);

/// <summary>
/// Today's board. <paramref name="Live"/> is true only when the rows came
/// from the airport's own feed on this request (or its short cache).
/// </summary>
public sealed record FlightBoardDto(
    bool Live,
    DateTimeOffset RetrievedAt,
    IReadOnlyList<FlightRowDto> Arrivals,
    IReadOnlyList<FlightRowDto> Departures);
