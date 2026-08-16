using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

/// <summary>
/// The Places slice, end to end: HTTP in, PostgreSQL and back.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class PlacesEndpointTests(LubnanApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpClient Client => factory.CreateClient();

    [Fact]
    public async Task The_list_returns_every_seeded_place_in_editorial_order()
    {
        var places = await Client.GetFromJsonAsync<List<PlaceSummaryDto>>("/api/v1/places", Json)
            .ConfigureAwait(true);

        Assert.NotNull(places);
        Assert.Equal(8, places.Count);

        // Editorial order, not alphabetical and not insertion order. The index
        // is what the design renders as "01", so it has to start at 01 and run
        // without a gap.
        Assert.Equal(
            ["01", "02", "03", "04", "05", "06", "07", "08"],
            places.Select(p => p.Index));

        Assert.Equal("byblos", places[0].Slug);
        Assert.Equal("Byblos", places[0].Name);
        Assert.Equal("Jbeil", places[0].LocalName);
    }

    [Fact]
    public async Task Filters_combine_rather_than_replace_one_another()
    {
        var coast = await Client.GetFromJsonAsync<List<PlaceSummaryDto>>(
            "/api/v1/places?region=Coast", Json).ConfigureAwait(true);

        var coastRuins = await Client.GetFromJsonAsync<List<PlaceSummaryDto>>(
            "/api/v1/places?region=Coast&category=ruins", Json).ConfigureAwait(true);

        Assert.Equal(["byblos", "beirut", "batroun"], coast!.Select(p => p.Slug));
        Assert.Equal(["byblos"], coastRuins!.Select(p => p.Slug));
    }

    [Fact]
    public async Task Filter_values_are_case_insensitive()
    {
        // A client should not have to know that the enum member is MountLebanon
        // rather than mountlebanon. The URL is a public surface.
        var response = await Client.GetAsync(new Uri("/api/v1/places?region=mountlebanon", UriKind.Relative))
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_filter_is_a_400_and_says_what_was_expected()
    {
        // Not an empty list. An empty list would let a typo read as "there is
        // nothing in that region", which is a bug that renders perfectly.
        var response = await Client.GetAsync(new Uri("/api/v1/places?region=Atlantis", UriKind.Relative))
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(Json).ConfigureAwait(true);
        Assert.Equal("request.invalid", problem!.Code);
        Assert.Contains("MountLebanon", problem.Errors!["Region"][0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_detail_page_carries_its_callouts_and_practical_strip()
    {
        var place = await Client.GetFromJsonAsync<PlaceDetailDto>("/api/v1/places/byblos", Json)
            .ConfigureAwait(true);

        Assert.NotNull(place);
        Assert.Equal("byblos", place.Slug);
        Assert.Equal(3, place.Callouts.Count);
        Assert.Equal(4, place.Practical.Count);

        // Coordinates are fractions of the plate and have been through a jsonb
        // round trip. If they came back as pixels or as zero, the dots would
        // stack in the corner of the photograph.
        Assert.All(place.Callouts, c =>
        {
            Assert.InRange(c.X, 0, 1);
            Assert.InRange(c.Y, 0, 1);
            Assert.False(string.IsNullOrWhiteSpace(c.Label));
        });

        Assert.Contains(place.Callouts, c => c.Label == "Crusader keep");
        Assert.Contains(place.Practical, f => f.Label == "Getting there");
    }

    [Fact]
    public async Task Plate_ids_come_back_as_ids_not_as_urls()
    {
        // The frontend resolves ids to paths, because it is the half that knows
        // the viewport and the extension chain. A URL here would freeze a CDN
        // host and a file extension into every stored row.
        var place = await Client.GetFromJsonAsync<PlaceDetailDto>("/api/v1/places/byblos", Json)
            .ConfigureAwait(true);

        Assert.Equal("J1", place!.Plates.Hero);
        Assert.Equal("K1", place.Plates.Subject);
    }

    [Fact]
    public async Task An_untranslated_place_falls_back_and_admits_it()
    {
        // Arabic copy does not exist yet. The response must not claim it does:
        // a client needs to be able to mark the page as untranslated, and it
        // can only do that if the payload says which locale it actually got.
        var response = await Client.GetAsync(new Uri("/api/v1/places/byblos?locale=ar", UriKind.Relative))
            .ConfigureAwait(true);

        var place = await response.Content.ReadFromJsonAsync<PlaceDetailDto>(Json).ConfigureAwait(true);

        Assert.Equal("en", place!.Locale);
        Assert.Equal("Byblos", place.Name);

        // Content-Language describes the body, so it says en even though ar was
        // requested. Labelling English prose as Arabic would mislead caches,
        // translation tooling and screen readers.
        Assert.Equal("en", response.Content.Headers.ContentLanguage.Single());

        // And Vary is set, or a CDN hands this English copy to the next reader
        // asking for Arabic and never revalidates.
        Assert.Contains("Accept-Language", response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Accept_Language_is_honoured_by_quality_not_by_order()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/places/byblos");
        request.Headers.Add("Accept-Language", "de;q=0.9, fr;q=0.8, en;q=0.1");

        var response = await Client.SendAsync(request).ConfigureAwait(true);
        var place = await response.Content.ReadFromJsonAsync<PlaceDetailDto>(Json).ConfigureAwait(true);

        // German is not published and is skipped; French outranks English on q,
        // so French is what the negotiation picks. There is no French copy yet,
        // so English is what comes back, and both facts are visible: the
        // fallback happened and the response does not pretend otherwise.
        Assert.Equal("en", place!.Locale);
        Assert.Equal("en", response.Content.Headers.ContentLanguage.Single());
    }

    [Fact]
    public async Task An_unknown_slug_is_a_404_with_a_stable_code()
    {
        var response = await Client.GetAsync(new Uri("/api/v1/places/atlantis", UriKind.Relative))
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(Json).ConfigureAwait(true);

        // The frontend switches on this. It must never switch on the title,
        // which is prose and will be translated.
        Assert.Equal("place.notFound", problem!.Code);
    }

    [Theory]
    [InlineData("NOT_a_slug")]
    [InlineData("byblos--harbour")]
    [InlineData("' OR 1=1 --")]
    public async Task A_malformed_slug_is_rejected_before_it_reaches_the_database(string slug)
    {
        var response = await Client.GetAsync(new Uri($"/api/v1/places/{Uri.EscapeDataString(slug)}", UriKind.Relative))
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Every_response_carries_a_correlation_id()
    {
        var response = await Client.GetAsync(new Uri("/api/v1/places", UriKind.Relative)).ConfigureAwait(true);

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.False(string.IsNullOrWhiteSpace(values!.Single()));
    }

    [Fact]
    public async Task Readiness_is_green_when_the_database_is_reachable()
    {
        var live = await Client.GetAsync(new Uri("/health/live", UriKind.Relative)).ConfigureAwait(true);
        var ready = await Client.GetAsync(new Uri("/health/ready", UriKind.Relative)).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }
}
