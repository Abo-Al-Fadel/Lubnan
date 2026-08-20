using Lubnan.Application.Abstractions;
using Microsoft.Extensions.Options;
using Lubnan.Application.Features.Flights;

namespace Lubnan.Infrastructure.Flights;

/// <summary>
/// Live board from the airport's public flight page, with a short cache and
/// a static fallback so a blip at beirutairport.gov.lb does not empty /plan.
/// </summary>
public sealed class FlightOptions
{
    public const string SectionName = "Flights";

    /// <summary>
    /// Whether to read the airport's page at all.
    /// </summary>
    /// <remarks>
    /// A kill switch, because this is the one part of the system that depends
    /// on somebody else's HTML. When beirutairport.gov.lb changes their markup
    /// - not if - the parser stops finding rows and /plan falls back to the
    /// static schedule anyway. But if their page starts answering slowly rather
    /// than differently, every board request pays the eight-second timeout
    /// before falling back.
    ///
    /// Setting Flights__Enabled=false skips the fetch entirely and serves the
    /// fallback immediately. It is a configuration change on a running service
    /// rather than a deploy, which is the difference between a two-minute fix
    /// and a twenty-minute one at the point where somebody has noticed.
    /// </remarks>
    public bool Enabled { get; set; } = true;
}

internal sealed class BeirutAirportFlightBoard(
    HttpClient http,
    IOptions<FlightOptions> options,
    IClock clock) : IFlightBoard
{
    internal const string ArrivalsPath = "_flight.php?lang=en&type=arivl";
    internal const string DeparturesPath = "_flight.php?lang=en&type=dprtr";

    private static readonly TimeSpan LiveFor = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan FallbackFor = TimeSpan.FromSeconds(30);
    private const int MaxBytes = 1_000_000;

    // Typed HttpClient is transient. The board must live across instances
    // or every /flights request scrapes the airport twice.
    private static readonly SemaphoreSlim Refresh = new(1, 1);
    private static readonly object Gate = new();
    private static FlightBoardDto? Cached;
    private static DateTimeOffset Until;

    public async Task<FlightBoardDto> GetAsync(CancellationToken cancellationToken)
    {
        if (TryHit(clock.UtcNow, out var hit))
        {
            return hit;
        }

        await Refresh.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = clock.UtcNow;
            if (TryHit(now, out hit))
            {
                return hit;
            }

            FlightBoardDto next;

            if (!options.Value.Enabled)
            {
                // Switched off. Serve the static schedule and cache it for the
                // live duration rather than the short fallback one - there is
                // nothing to retry towards.
                next = FallbackSchedule.Board(now);
                Store(next, now + LiveFor);
                return next;
            }

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
        finally
        {
            Refresh.Release();
        }
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

    private static bool TryHit(DateTimeOffset now, out FlightBoardDto hit)
    {
        lock (Gate)
        {
            if (Cached is not null && now < Until)
            {
                hit = Cached;
                return true;
            }
        }

        hit = null!;
        return false;
    }

    private static void Store(FlightBoardDto value, DateTimeOffset until)
    {
        lock (Gate)
        {
            Cached = value;
            Until = until;
        }
    }
}
