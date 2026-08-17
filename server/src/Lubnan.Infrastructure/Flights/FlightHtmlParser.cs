using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Lubnan.Application.Features.Flights;

namespace Lubnan.Infrastructure.Flights;

/// <summary>
/// Reads the public FIDS table on beirutairport.gov.lb.
/// </summary>
/// <remarks>
/// The airport publishes this HTML for travellers. We take text only — no
/// markup is forwarded — and drop a row that does not look like a flight
/// rather than guessing.
/// </remarks>
public static partial class FlightHtmlParser
{
    public const int MaxRows = 200;
    private const int MaxField = 80;

    public static IReadOnlyList<FlightRowDto> Parse(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var table = TablePattern().Match(html);
        if (!table.Success)
        {
            return [];
        }

        var rows = new List<FlightRowDto>(64);
        foreach (Match row in RowPattern().Matches(table.Groups[1].Value))
        {
            if (rows.Count >= MaxRows)
            {
                break;
            }

            var cells = CellPattern().Matches(row.Value);
            if (cells.Count < 9)
            {
                continue;
            }

            var airline = Clip(ImgTitle(cells[0].Groups[1].Value) ?? Text(cells[0].Groups[1].Value));
            var time = Clip(Text(cells[1].Groups[1].Value));
            var code = Clip(Text(cells[2].Groups[1].Value));
            var city = Title(Text(cells[3].Groups[1].Value));
            var country = Title(Text(cells[4].Groups[1].Value));
            var gate = Clip(Text(cells[6].Groups[1].Value));
            var statusRaw = Text(cells[7].Groups[1].Value);
            var real = Clip(Text(cells[8].Groups[1].Value));

            if (code.Length == 0 || time.Length == 0 || city.Length == 0)
            {
                continue;
            }

            var status = MapStatus(statusRaw);
            rows.Add(new FlightRowDto(
                Code: code,
                Airline: airline.Length == 0 ? code.Split(' ')[0] : airline,
                Iata: CityIata.Lookup(city),
                City: city,
                Country: country,
                Time: time,
                Delay: DelayMinutes(time, real, status),
                Status: status,
                Terminal: string.Empty,
                Gate: gate));
        }

        return rows;
    }

    private static string MapStatus(string raw)
    {
        var value = raw.Trim().ToLowerInvariant();
        if (value.Contains("cancel", StringComparison.Ordinal))
        {
            return "cancelled";
        }

        if (value.Contains("delay", StringComparison.Ordinal))
        {
            return "delayed";
        }

        if (value.Contains("arriv", StringComparison.Ordinal) || value.Contains("land", StringComparison.Ordinal))
        {
            return "landed";
        }

        if (value.Contains("depart", StringComparison.Ordinal) || value.Contains("gate closed", StringComparison.Ordinal))
        {
            return "departed";
        }

        if (value.Contains("board", StringComparison.Ordinal)
            || value.Contains("gate open", StringComparison.Ordinal)
            || value.Contains("final call", StringComparison.Ordinal))
        {
            return "boarding";
        }

        return "on-time";
    }

    private static int DelayMinutes(string scheduled, string actual, string status)
    {
        if (status != "delayed")
        {
            return 0;
        }

        if (!TryParseClock(scheduled, out var start) || !TryParseClock(actual, out var end))
        {
            return 0;
        }

        var delta = end - start;
        if (delta < TimeSpan.Zero)
        {
            delta += TimeSpan.FromDays(1);
        }

        return (int)Math.Clamp(delta.TotalMinutes, 0, 24 * 60);
    }

    private static bool TryParseClock(string value, out TimeSpan clock)
    {
        return TimeSpan.TryParseExact(value, @"h\:mm", CultureInfo.InvariantCulture, out clock)
               || TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out clock);
    }

    private static string Text(string html)
    {
        var stripped = TagPattern().Replace(html, " ");
        return WebUtility.HtmlDecode(stripped).Replace('\u00a0', ' ').Trim();
    }

    private static string? ImgTitle(string html)
    {
        var match = ImgTitlePattern().Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : null;
    }

    private static string Title(string value)
    {
        var clipped = Clip(value);
        if (clipped.Length == 0)
        {
            return clipped;
        }

        var letters = clipped.Where(char.IsLetter).ToArray();
        if (letters.Length > 0 && letters.All(char.IsUpper))
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(clipped.ToLowerInvariant());
        }

        return clipped;
    }

    private static string Clip(string value) =>
        value.Length <= MaxField ? value : value[..MaxField];

    [GeneratedRegex(@"class=['""]flight_table['""][^>]*>(.*)</table>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TablePattern();

    [GeneratedRegex(@"<tr\b[^>]*>.*?</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex RowPattern();

    [GeneratedRegex(@"<td\b[^>]*>(.*?)</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CellPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"title=['""]([^'""]+)['""]", RegexOptions.IgnoreCase)]
    private static partial Regex ImgTitlePattern();
}
