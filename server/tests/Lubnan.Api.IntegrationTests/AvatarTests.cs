using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

/// <summary>
/// Profile pictures, and what happens to an upload on the way in.
/// </summary>
/// <remarks>
/// The point of these is not that a valid picture works — that is the easy
/// half. It is that a file which is <em>also</em> something else stops being
/// something else, and that a small file describing an enormous image is
/// refused before it is decoded.
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class AvatarTests(LubnanApiFactory factory)
{
    private const string Password = "a long enough passphrase";

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private sealed record Session(HttpClient Client, string Csrf, Guid UserId);

    private async Task<Session> SignedInAsync()
    {
        var client = NewClient();
        var email = $"avatar-{Guid.NewGuid():N}@example.com";

        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = Password, displayName = "Avatar QA" }).ConfigureAwait(true);
        await factory.ConfirmAsync(email).ConfigureAwait(true);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = Password }).ConfigureAwait(true);

        var me = await client.GetFromJsonAsync<MeDto>("/api/v1/me",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
            .ConfigureAwait(true);

        return new Session(client, LubnanApiFactory.CsrfFrom(login), me!.Id);
    }

    private static byte[] Png(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var buffer = new MemoryStream();
        image.Save(buffer, new PngEncoder());
        return buffer.ToArray();
    }

    private static async Task<HttpResponseMessage> UploadAsync(Session s, byte[] bytes, string name, string type)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(type);
        content.Add(file, "file", name);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/avatar") { Content = content };
        request.Headers.Add("X-CSRF-Token", s.Csrf);

        return await s.Client.SendAsync(request).ConfigureAwait(false);
    }

    [Fact]
    public async Task A_real_picture_is_accepted_and_served_back_as_webp()
    {
        var s = await SignedInAsync().ConfigureAwait(true);

        var uploaded = await UploadAsync(s, Png(600, 400), "me.png", "image/png").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);

        var served = await NewClient()
            .GetAsync(new Uri($"/api/v1/users/{s.UserId}/avatar", UriKind.Relative))
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, served.StatusCode);

        // WebP, not PNG. What is stored is what we encoded, not what arrived.
        Assert.Equal("image/webp", served.Content.Headers.ContentType?.MediaType);

        var body = await served.Content.ReadAsByteArrayAsync().ConfigureAwait(true);

        // Square at the size the domain declares, whatever shape went in.
        var info = Image.Identify(body);
        Assert.Equal(Lubnan.Domain.Users.Avatar.Size, info.Width);
        Assert.Equal(Lubnan.Domain.Users.Avatar.Size, info.Height);
    }

    [Fact]
    public async Task A_file_pretending_to_be_an_image_is_refused()
    {
        var s = await SignedInAsync().ConfigureAwait(true);

        // Correct extension, correct declared content type, and the bytes are a
        // script. Neither the name nor the header is evidence of anything.
        var payload = Encoding.UTF8.GetBytes("<script>alert(document.cookie)</script>");

        var response = await UploadAsync(s, payload, "avatar.png", "image/png").ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_polyglot_stops_being_a_polyglot()
    {
        var s = await SignedInAsync().ConfigureAwait(true);

        // A genuine PNG with a script appended: valid image, and valid HTML to
        // anything that sniffs. Decoding keeps the pixels and discards
        // everything that was not pixels, so the trailing half cannot survive.
        var png = Png(300, 300);
        var script = Encoding.UTF8.GetBytes("<script>alert(1)</script>");
        var polyglot = png.Concat(script).ToArray();

        var uploaded = await UploadAsync(s, polyglot, "ok.png", "image/png").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);

        var served = await NewClient()
            .GetAsync(new Uri($"/api/v1/users/{s.UserId}/avatar", UriKind.Relative))
            .ConfigureAwait(true);

        var body = await served.Content.ReadAsByteArrayAsync().ConfigureAwait(true);
        var text = Encoding.UTF8.GetString(body);

        Assert.DoesNotContain("<script>", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_decompression_bomb_is_refused_before_it_is_decoded()
    {
        var s = await SignedInAsync().ConfigureAwait(true);

        // A few kilobytes of PNG describing an image far past the ceiling. The
        // upload cap cannot catch this - the file genuinely is small - so the
        // dimensions have to be read from the header and checked before the
        // body is turned into a buffer.
        var bomb = Png(20_000, 20_000);

        var response = await UploadAsync(s, bomb, "big.png", "image/png").ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_set_one()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Png(100, 100)), "file", "me.png");

        var response = await NewClient()
            .PostAsync(new Uri("/api/v1/me/avatar", UriKind.Relative), content)
            .ConfigureAwait(true);

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"expected 401 or 403, got {(int)response.StatusCode}");
    }

    /// <summary>
    /// The failure a real person is most likely to cause, answered as theirs.
    /// </summary>
    /// <remarks>
    /// A photograph straight off a phone clears four megabytes routinely, so
    /// this is not an edge case — it is the ordinary way the upload fails. The
    /// limit is enforced while the body is being read, which means it throws
    /// rather than returns, and an escaped exception used to leave as 500
    /// "Something went wrong on our side": the one message that is both untrue
    /// and impossible to act on. The profile page renders whatever sentence the
    /// server sends, so the status here is what decides whether somebody
    /// resizes their picture or files a bug.
    /// </remarks>
    [Fact]
    public async Task An_upload_past_the_limit_is_the_callers_problem_not_a_server_fault()
    {
        var s = await SignedInAsync().ConfigureAwait(true);

        var oversize = new byte[Lubnan.Domain.Users.Avatar.MaxUploadBytes + (256 * 1024)];
        var response = await UploadAsync(s, oversize, "holiday.png", "image/png").ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("avatar.tooLarge", body, StringComparison.Ordinal);

        // And it says the size, because "too large" without a number leaves
        // somebody guessing how much smaller is small enough.
        Assert.Contains("4 MB", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Removing_it_leaves_nothing_to_serve()
    {
        var s = await SignedInAsync().ConfigureAwait(true);
        await UploadAsync(s, Png(400, 400), "me.png", "image/png").ConfigureAwait(true);

        var removed = await LubnanApiFactory
            .DeleteWithCsrfAsync(s.Client, "/api/v1/me/avatar", s.Csrf)
            .ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        var served = await NewClient()
            .GetAsync(new Uri($"/api/v1/users/{s.UserId}/avatar", UriKind.Relative))
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.NotFound, served.StatusCode);
    }
}
