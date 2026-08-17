using Lubnan.Application.Abstractions;
using Lubnan.Application.Features.Flights;

namespace Lubnan.Infrastructure.Flights;

/// <summary>
/// Live board from the airport's public flight page, with a short cache and
/// a static fallback so a blip at beirutairport.gov.lb does not empty /plan.
/// </summary>
internal sealed class BeirutAirportFlightBoard(HttpClient http, IClock clock) : IFlightBoard
{
    internal const string ArrivalsPath = "_flight.php?lang=en&type=arivl";
    internal const string DeparturesPath = "_flight.php?lang=en&type=dprtr";

    private static readonly TimeSpan LiveFor = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan FallbackFor = TimeSpan.FromSeconds(30);
    private const int MaxBytes = 1_000_000;

    private readonly object _gate = new();
    private FlightBoardDto? _cached;
    private DateTimeOffset _until;

    public async Task<FlightBoardDto> GetAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        lock (_gate)
        {
            if (_cached is not null && now < _until)
            {
                return _cached;
            }
        }

        FlightBoardDto next;
        try
        {
            var arrivals = await ReadAsync(ArrivalsPath, cancellationToken).ConfigureAwait(false);
            var departures = await ReadAsync(DeparturesPath, cancellationToken).ConfigureAwait(false);
            if (arrivals.Count == 0 && departures.Count == 0)
            {
                throw new InvalidOperationException("The airport page had no flight rows.");
            }

            next = new FlightBoardDto(true, now, arrivals, departures);
            Store(next, now + LiveFor);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            next = FallbackSchedule.Board(now);
            Store(next, now + FallbackFor);
        }

        return next;
    }

    private async Task<IReadOnlyList<FlightRowDto>> ReadAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var length = response.Content.Headers.ContentLength;
        if (length > MaxBytes)
        {
            throw new InvalidOperationException("The airport page is larger than expected.");
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (html.Length > MaxBytes)
        {
            html = html[..MaxBytes];
        }

        return FlightHtmlParser.Parse(html);
    }

    private void Store(FlightBoardDto value, DateTimeOffset until)
    {
        lock (_gate)
        {
            _cached = value;
            _until = until;
        }
    }
}
