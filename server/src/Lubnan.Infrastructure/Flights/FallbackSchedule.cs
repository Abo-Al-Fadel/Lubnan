using Lubnan.Application.Features.Flights;

namespace Lubnan.Infrastructure.Flights;

/// <summary>
/// The route network out of BEY, used only when the airport site cannot be
/// reached. Times are illustrative; the board says so via <c>Live = false</c>.
/// </summary>
internal static class FallbackSchedule
{
    public static FlightBoardDto Board(DateTimeOffset now) => new(
        Live: false,
        RetrievedAt: now,
        Arrivals:
        [
            Row("ME 202", "Middle East Airlines", "CDG", "Paris", "France", "06:55", 0, "landed", "A4"),
            Row("TK 828", "Turkish Airlines", "IST", "Istanbul", "Türkiye", "07:40", 0, "landed", "B2"),
            Row("QR 418", "Qatar Airways", "DOH", "Doha", "Qatar", "08:35", 20, "delayed", "B5"),
            Row("ME 218", "Middle East Airlines", "LHR", "London", "United Kingdom", "09:15", 0, "on-time", "A7"),
            Row("MS 708", "EgyptAir", "CAI", "Cairo", "Egypt", "10:50", 0, "on-time", "B1"),
            Row("LH 1286", "Lufthansa", "FRA", "Frankfurt", "Germany", "12:05", 0, "on-time", "B8"),
            Row("ME 266", "Middle East Airlines", "DXB", "Dubai", "UAE", "13:40", 0, "on-time", "A9"),
            Row("EK 957", "Emirates", "DXB", "Dubai", "UAE", "15:10", 0, "on-time", "B6"),
            Row("AF 562", "Air France", "CDG", "Paris", "France", "16:20", 0, "on-time", "B4"),
            Row("ME 430", "Middle East Airlines", "AMM", "Amman", "Jordan", "17:35", 0, "on-time", "A2"),
            Row("ME 374", "Middle East Airlines", "ATH", "Athens", "Greece", "19:50", 0, "on-time", "A5"),
            Row("ME 316", "Middle East Airlines", "FCO", "Rome", "Italy", "21:15", 0, "on-time", "A3"),
        ],
        Departures:
        [
            Row("ME 201", "Middle East Airlines", "CDG", "Paris", "France", "07:45", 0, "departed", "A4"),
            Row("ME 217", "Middle East Airlines", "LHR", "London", "United Kingdom", "08:20", 0, "boarding", "A7"),
            Row("TK 829", "Turkish Airlines", "IST", "Istanbul", "Türkiye", "09:05", 0, "on-time", "B2"),
            Row("ME 265", "Middle East Airlines", "DXB", "Dubai", "UAE", "09:40", 25, "delayed", "A9"),
            Row("QR 419", "Qatar Airways", "DOH", "Doha", "Qatar", "10:15", 0, "on-time", "B5"),
            Row("MS 707", "EgyptAir", "CAI", "Cairo", "Egypt", "11:30", 0, "on-time", "B1"),
            Row("ME 315", "Middle East Airlines", "FCO", "Rome", "Italy", "12:10", 0, "on-time", "A3"),
            Row("LH 1287", "Lufthansa", "FRA", "Frankfurt", "Germany", "13:25", 0, "on-time", "B8"),
            Row("ME 429", "Middle East Airlines", "AMM", "Amman", "Jordan", "14:00", 15, "delayed", "A2"),
            Row("AF 563", "Air France", "CDG", "Paris", "France", "15:45", 0, "on-time", "B4"),
            Row("EK 958", "Emirates", "DXB", "Dubai", "UAE", "16:30", 0, "on-time", "B6"),
            Row("ME 373", "Middle East Airlines", "ATH", "Athens", "Greece", "18:05", 0, "on-time", "A5"),
        ]);

    private static FlightRowDto Row(
        string code,
        string airline,
        string iata,
        string city,
        string country,
        string time,
        int delay,
        string status,
        string gate) =>
        new(code, airline, iata, city, country, time, delay, status, string.Empty, gate);
}
