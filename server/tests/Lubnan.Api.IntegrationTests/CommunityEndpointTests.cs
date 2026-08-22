using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Features.Community;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class CommunityEndpointTests(LubnanApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpClient Client => factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

    [Fact]
    public async Task The_feed_is_public()
    {
        var response = await Client.GetAsync(new Uri("/api/v1/community/posts", UriKind.Relative))
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var feed = await response.Content.ReadFromJsonAsync<List<PostDto>>(Json).ConfigureAwait(true);
        Assert.NotNull(feed);
        Assert.NotEmpty(feed);
        Assert.All(feed, post => Assert.False(post.LikedByMe));
    }

    [Fact]
    public async Task Liking_without_a_session_is_unauthorized()
    {
        var feed = await Client.GetFromJsonAsync<List<PostDto>>("/api/v1/community/posts", Json)
            .ConfigureAwait(true);
        var id = feed![0].Id;

        var response = await Client
            .PostAsync(new Uri($"/api/v1/community/posts/{id}/like", UriKind.Relative), null)
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_signed_in_reader_can_post_like_and_comment()
    {
        var (client, csrf) = await SignInAsync().ConfigureAwait(true);

        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/community/posts");
        create.Headers.TryAddWithoutValidation(AuthCookies.CsrfHeader, csrf);
        create.Content = JsonContent.Create(new
        {
            body = "First light at the harbour. The boats were already out.",
            placeSlug = "byblos",
        });

        var created = await client.SendAsync(create).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var post = await created.Content.ReadFromJsonAsync<PostDto>(Json).ConfigureAwait(true);
        Assert.NotNull(post);
        Assert.Equal("byblos", post.PlaceSlug);
        Assert.Equal(0, post.LikeCount);

        using var like = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/community/posts/{post.Id}/like");
        like.Headers.TryAddWithoutValidation(AuthCookies.CsrfHeader, csrf);
        var liked = await client.SendAsync(like).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, liked.StatusCode);
        var state = await liked.Content.ReadFromJsonAsync<LikeStateDto>(Json).ConfigureAwait(true);
        Assert.True(state!.Liked);
        Assert.Equal(1, state.LikeCount);

        using var comment = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/community/posts/{post.Id}/comments");
        comment.Headers.TryAddWithoutValidation(AuthCookies.CsrfHeader, csrf);
        comment.Content = JsonContent.Create(new { body = "I walked that harbour last April." });
        var replied = await client.SendAsync(comment).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, replied.StatusCode);
        var reply = await replied.Content.ReadFromJsonAsync<CommentDto>(Json).ConfigureAwait(true);
        Assert.True(reply!.Mine);
        Assert.Equal("I walked that harbour last April.", reply.Body);

        var feed = await client.GetFromJsonAsync<List<PostDto>>("/api/v1/community/posts", Json)
            .ConfigureAwait(true);
        var mine = feed!.Single(p => p.Id == post.Id);
        Assert.True(mine.LikedByMe);
        Assert.Equal(1, mine.LikeCount);
        Assert.Contains(mine.Comments, c => c.Id == reply.Id);
    }

    [Fact]
    public async Task A_region_filter_only_returns_that_region()
    {
        var feed = await Client
            .GetFromJsonAsync<List<PostDto>>("/api/v1/community/posts?region=Bekaa", Json)
            .ConfigureAwait(true);

        Assert.NotNull(feed);
        Assert.NotEmpty(feed);
        Assert.All(feed, post => Assert.Equal("Bekaa", post.Region));
    }

    [Fact]
    public async Task The_body_cannot_choose_another_author()
    {
        var (client, csrf) = await SignInAsync().ConfigureAwait(true);
        var me = await client.GetFromJsonAsync<MeDto>("/api/v1/me", Json).ConfigureAwait(true);

        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/community/posts");
        create.Headers.TryAddWithoutValidation(AuthCookies.CsrfHeader, csrf);
        create.Content = JsonContent.Create(new
        {
            body = "Trying to post as someone else.",
            authorId = Guid.NewGuid(),
        });

        var created = await client.SendAsync(create).ConfigureAwait(true);
        var post = await created.Content.ReadFromJsonAsync<PostDto>(Json).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(me!.Id, post!.Author.Id);
    }

    /// <summary>
    /// A face reaches the feed, and the feed does not carry the picture.
    /// </summary>
    /// <remarks>
    /// Two assertions and they are opposite halves of one decision. The version
    /// has to be there, or the client cannot tell "no picture" from "picture I
    /// have not asked for" and puts a 404 in the console behind every member
    /// who never set one. And the bytes must <em>not</em> be there: avatars are
    /// rows in this database rather than objects in a bucket, so a projection
    /// that reached for the entity would answer a question about eighty
    /// timestamps by loading eighty images.
    /// </remarks>
    [Fact]
    public async Task The_feed_carries_a_version_for_each_face_and_none_of_the_pixels()
    {
        var (client, csrf) = await SignInAsync().ConfigureAwait(true);
        var me = await client.GetFromJsonAsync<MeDto>("/api/v1/me", Json).ConfigureAwait(true);

        using var picture = new System.Net.Http.MultipartFormDataContent();
        var bytes = new ByteArrayContent(OnePixelPng());
        bytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        picture.Add(bytes, "file", "me.png");

        using var upload = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/avatar")
        {
            Content = picture,
        };
        upload.Headers.Add(AuthCookies.CsrfHeader, csrf);
        var uploaded = await client.SendAsync(upload).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);

        using var write = new HttpRequestMessage(HttpMethod.Post, "/api/v1/community/posts")
        {
            Content = JsonContent.Create(new { body = "A post from somebody with a face.", placeSlug = (string?)null }),
        };
        write.Headers.Add(AuthCookies.CsrfHeader, csrf);
        var created = await client.SendAsync(write).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        // The version is on the DTO the write returns, not only on the next
        // read. The post is spliced straight into the page, so without it the
        // author's own new post is the one row showing initials.
        var fresh = await created.Content.ReadFromJsonAsync<PostDto>(Json).ConfigureAwait(true);
        Assert.False(string.IsNullOrEmpty(fresh!.Author.AvatarVersion));

        var raw = await Client.GetStringAsync(new Uri("/api/v1/community/posts", UriKind.Relative))
            .ConfigureAwait(true);

        var feed = JsonSerializer.Deserialize<List<PostDto>>(raw, Json)!;
        var mine = feed.Single(p => p.Id == fresh.Id);

        Assert.Equal(me!.Id, mine.Author.Id);
        Assert.Equal(fresh.Author.AvatarVersion, mine.Author.AvatarVersion);

        // Seeded posts belong to members who never uploaded anything, and null
        // is how the client knows to draw initials instead of asking.
        Assert.Contains(feed, p => p.Author.AvatarVersion is null);

        // And nothing image-shaped came back. A base64 PNG in the payload would
        // start "iVBORw0KGgo"; the field would be there whatever it was called.
        Assert.DoesNotContain("iVBORw0KGgo", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("content", raw, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] OnePixelPng() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private async Task<(HttpClient Client, string Csrf)> SignInAsync()
    {
        var client = Client;
        var email = $"reader-{Guid.NewGuid():N}@example.com";
        const string password = "a long enough passphrase";

        await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password, displayName = "Reader" }).ConfigureAwait(true);

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
