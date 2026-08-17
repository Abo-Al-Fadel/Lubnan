using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Features.SavedPlaces;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class SavedPlacesEndpointTests(LubnanApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpClient Client => factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

    [Fact]
    public async Task Saving_without_a_session_is_unauthorized()
    {
        var response = await Client
            .PostAsJsonAsync("/api/v1/me/saved", new { slug = "byblos" })
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_signed_in_reader_can_pin_and_unpin()
    {
        var (client, csrf) = await SignInAsync().ConfigureAwait(true);

        using var pin = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/saved");
        pin.Headers.TryAddWithoutValidation(AuthCookies.CsrfHeader, csrf);
        pin.Content = JsonContent.Create(new { slug = "byblos" });

        var created = await client.SendAsync(pin).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var row = await created.Content.ReadFromJsonAsync<SavedPlaceDto>(Json).ConfigureAwait(true);
        Assert.Equal("byblos", row!.Slug);

        var listed = await client.GetFromJsonAsync<List<SavedPlaceDto>>("/api/v1/me/saved", Json)
            .ConfigureAwait(true);
        Assert.Contains(listed!, s => s.Slug == "byblos");

        using var again = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/saved");
        again.Headers.TryAddWithoutValidation(AuthCookies.CsrfHeader, csrf);
        again.Content = JsonContent.Create(new { slug = "byblos" });
        var repeat = await client.SendAsync(again).ConfigureAwait(true);
        Assert.True(
            repeat.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK,
            repeat.StatusCode.ToString());

        using var unpin = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/me/saved/byblos");
        unpin.Headers.TryAddWithoutValidation(AuthCookies.CsrfHeader, csrf);
        var removed = await client.SendAsync(unpin).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        var empty = await client.GetFromJsonAsync<List<SavedPlaceDto>>("/api/v1/me/saved", Json)
            .ConfigureAwait(true);
        Assert.DoesNotContain(empty!, s => s.Slug == "byblos");
    }

    private async Task<(HttpClient Client, string Csrf)> SignInAsync()
    {
        var client = Client;
        var email = $"saver-{Guid.NewGuid():N}@example.com";
        const string password = "a long enough passphrase";

        await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password, displayName = "Saver" }).ConfigureAwait(true);

        await factory.ConfirmAsync(email).ConfigureAwait(true);

        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password }).ConfigureAwait(true);

        var csrf = login.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith(AuthCookies.CsrfCookie, StringComparison.Ordinal))
            .Split(';')[0]
            .Split('=')[1];

        return (client, csrf);
    }
}
