using Lubnan.Infrastructure.Flights;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

public sealed class FlightHtmlParserTests
{
    private const string Sample = """
        <table id='date_1' class='flight_table'>
          <thead>
            <tr class='date_row'><td colspan='9'>2026-08-18</td></tr>
            <tr class='tbl_hdr'>
              <th>Airline</th><th>Time</th><th>Flight No.</th><th>From</th>
              <th>Country</th><th>Via</th><th>Belt</th><th>Status</th><th>Real</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td><img alt='ME' title='Middle East Airlines'/></td>
              <td>00:20</td>
              <td>ME 1427</td>
              <td>DUBAI</td>
              <td>UAE</td>
              <td>&nbsp;</td>
              <td>02</td>
              <td>Arrived</td>
              <td>00:08</td>
            </tr>
            <tr>
              <td><img alt='QR' title='Qatar Airways'/></td>
              <td>08:35</td>
              <td>QR 418</td>
              <td>DOHA</td>
              <td>QATAR</td>
              <td>&nbsp;</td>
              <td>B5</td>
              <td>Delayed</td>
              <td>08:55</td>
            </tr>
            <tr>
              <td><img alt='FZ' title='FLYDUBAI'/></td>
              <td>09:25</td>
              <td>FZ 157</td>
              <td>DUBAI</td>
              <td>UAE</td>
              <td>&nbsp;</td>
              <td></td>
              <td>Cancelled For Today</td>
              <td>09:25</td>
            </tr>
          </tbody>
        </table>
        """;

    [Fact]
    public void Reads_airline_city_and_status_from_the_public_table()
    {
        var rows = FlightHtmlParser.Parse(Sample);

        Assert.Equal(3, rows.Count);

        Assert.Equal("ME 1427", rows[0].Code);
        Assert.Equal("Middle East Airlines", rows[0].Airline);
        Assert.Equal("Dubai", rows[0].City);
        Assert.Equal("DXB", rows[0].Iata);
        Assert.Equal("landed", rows[0].Status);
        Assert.Equal(0, rows[0].Delay);
        Assert.Equal("02", rows[0].Gate);

        Assert.Equal("delayed", rows[1].Status);
        Assert.Equal(20, rows[1].Delay);

        Assert.Equal("cancelled", rows[2].Status);
    }

    [Fact]
    public void An_unrelated_page_is_an_empty_list_not_a_throw()
    {
        Assert.Empty(FlightHtmlParser.Parse("<html><body>No board here.</body></html>"));
    }
}
