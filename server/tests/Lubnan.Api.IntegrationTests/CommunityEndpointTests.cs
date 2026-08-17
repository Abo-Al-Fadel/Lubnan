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
