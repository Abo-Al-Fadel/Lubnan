using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

/// <summary>
/// Deleting a comment: your own, or anyone's if you moderate.
/// </summary>
/// <remarks>
/// The rule lives in <c>CommunityPost.RemoveComment</c>, so what these assert
/// is that the endpoint routes to it and reports its refusals honestly — a 403
/// for somebody else's comment rather than a 404 that pretends it is missing,
/// because the requester can plainly see it on the page.
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class RemoveCommentTests(LubnanApiFactory factory)
{
    private const string Password = "a long enough passphrase";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static string UniqueEmail() => $"comment-{Guid.NewGuid():N}@example.com";

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private sealed record Session(HttpClient Client, string Csrf);

    private async Task<Session> SignedInAsync()
    {
        var client = NewClient();
        var email = UniqueEmail();

        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = Password, displayName = "Comment QA" }).ConfigureAwait(true);
        await factory.ConfirmAsync(email).ConfigureAwait(true);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = Password }).ConfigureAwait(true);

        return new Session(client, LubnanApiFactory.CsrfFrom(login));
    }

    private static async Task<(Guid PostId, Guid CommentId)> PostWithCommentAsync(Session s)
    {
        var created = await LubnanApiFactory.PostWithCsrfAsync(
            s.Client, "/api/v1/community/posts", s.Csrf,
            new { title = "QA post", body = "A body long enough to pass validation rules.", placeSlug = "byblos" })
            .ConfigureAwait(true);

        var post = await created.Content.ReadFromJsonAsync<IdDto>(Json).ConfigureAwait(true);

        var commented = await LubnanApiFactory.PostWithCsrfAsync(
            s.Client, $"/api/v1/community/posts/{post!.Id}/comments", s.Csrf,
            new { body = "A comment worth removing." }).ConfigureAwait(true);

        var comment = await commented.Content.ReadFromJsonAsync<IdDto>(Json).ConfigureAwait(true);

        return (post.Id, comment!.Id);
    }

    [Fact]
    public async Task An_author_can_remove_their_own_comment()
    {
        var s = await SignedInAsync().ConfigureAwait(true);
        var (postId, commentId) = await PostWithCommentAsync(s).ConfigureAwait(true);

        var removed = await LubnanApiFactory.DeleteWithCsrfAsync(
            s.Client, $"/api/v1/community/posts/{postId}/comments/{commentId}", s.Csrf).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
    }

    [Fact]
    public async Task Removing_it_twice_reports_it_gone_rather_than_succeeding_silently()
    {
        var s = await SignedInAsync().ConfigureAwait(true);
        var (postId, commentId) = await PostWithCommentAsync(s).ConfigureAwait(true);
        var url = $"/api/v1/community/posts/{postId}/comments/{commentId}";

        await LubnanApiFactory.DeleteWithCsrfAsync(s.Client, url, s.Csrf).ConfigureAwait(true);
        var again = await LubnanApiFactory.DeleteWithCsrfAsync(s.Client, url, s.Csrf).ConfigureAwait(true);

        // 404, unlike session revocation, which answers 204 for an id that is
        // not yours. The difference is what an id leaks: a session id is a
        // secret and a distinguishable answer would let one be enumerated,
        // whereas a comment id is on the page in front of everybody.
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task Somebody_elses_comment_is_forbidden()
    {
        var author = await SignedInAsync().ConfigureAwait(true);
        var (postId, commentId) = await PostWithCommentAsync(author).ConfigureAwait(true);

        var stranger = await SignedInAsync().ConfigureAwait(true);

        var response = await LubnanApiFactory.DeleteWithCsrfAsync(
            stranger.Client, $"/api/v1/community/posts/{postId}/comments/{commentId}", stranger.Csrf)
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_remove_anything()
    {
        var author = await SignedInAsync().ConfigureAwait(true);
        var (postId, commentId) = await PostWithCommentAsync(author).ConfigureAwait(true);

        var response = await NewClient()
            .DeleteAsync(new Uri($"/api/v1/community/posts/{postId}/comments/{commentId}", UriKind.Relative))
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed record IdDto(Guid Id);
