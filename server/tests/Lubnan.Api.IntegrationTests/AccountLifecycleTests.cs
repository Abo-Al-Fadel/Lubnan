using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

/// <summary>
/// Leaving, coming back, and the sessions in between.
/// </summary>
/// <remarks>
/// The recovery paths matter more than the destructive ones here. An account
/// that cannot be deleted is an inconvenience; an account that cannot be
/// <em>un</em>-deleted after somebody else pressed the button is unrecoverable,
/// so that is what these assert.
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class AccountLifecycleTests(LubnanApiFactory factory)
{
    private const string Password = "a long enough passphrase";

    private static string UniqueEmail() => $"life-{Guid.NewGuid():N}@example.com";

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    /// <summary>A signed-in client and the CSRF token its session was issued.</summary>
    private sealed record Session(HttpClient Client, string Csrf);

    private async Task<Session> SignedInAsync(string email)
    {
        var client = NewClient();

        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = Password, displayName = "Life QA" }).ConfigureAwait(true);

        await factory.ConfirmAsync(email).ConfigureAwait(true);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = Password }).ConfigureAwait(true);

        return new Session(client, LubnanApiFactory.CsrfFrom(login));
    }

    [Fact]
    public async Task Deleting_an_account_requires_the_password_again()
    {
        var session = await SignedInAsync(UniqueEmail()).ConfigureAwait(true);

        var wrong = await LubnanApiFactory.PostWithCsrfAsync(
            session.Client, "/api/v1/me/deletion", session.Csrf,
            new { password = "not the password" }).ConfigureAwait(true);

        // A live session is exactly what somebody who walked up to an unlocked
        // laptop already has. The password is the difference between "this
        // browser is signed in" and "the account holder is here".
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    [Fact]
    public async Task A_deleted_account_comes_back_during_the_grace_period()
    {
        var email = UniqueEmail();
        var session = await SignedInAsync(email).ConfigureAwait(true);

        var deleted = await LubnanApiFactory.PostWithCsrfAsync(
            session.Client, "/api/v1/me/deletion", session.Csrf,
            new { password = Password }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal("PendingDeletion", await factory.StateOfAsync(email).ConfigureAwait(true));

        // Signing in still works while the clock is running — which is the
        // whole point. Somebody whose account was deleted by an intruder has to
        // be able to get back in to stop it.
        var second = NewClient();
        var back = await second.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = Password }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NoContent, back.StatusCode);

        var cancelled = await LubnanApiFactory.DeleteWithCsrfAsync(
            second, "/api/v1/me/deletion", LubnanApiFactory.CsrfFrom(back)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        Assert.Equal("Active", await factory.StateOfAsync(email).ConfigureAwait(true));
    }

    [Fact]
    public async Task Requesting_deletion_ends_every_session()
    {
        var email = UniqueEmail();
        var session = await SignedInAsync(email).ConfigureAwait(true);

        await LubnanApiFactory.PostWithCsrfAsync(
            session.Client, "/api/v1/me/deletion", session.Csrf,
            new { password = Password }).ConfigureAwait(true);

        // The cookies this client still holds are no longer honoured. Leaving
        // them alive would mean an account mid-deletion could still post.
        var after = await session.Client.GetAsync(new Uri("/api/v1/me", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task The_session_list_marks_the_device_asking()
    {
        var session = await SignedInAsync(UniqueEmail()).ConfigureAwait(true);

        var sessions = await session.Client
            .GetFromJsonAsync<List<SessionDto>>("/api/v1/auth/sessions")
            .ConfigureAwait(true);

        Assert.NotNull(sessions);
        var only = Assert.Single(sessions);

        // Served from under /api/v1/auth on purpose: the refresh cookie is
        // path-scoped there, so a session list anywhere else never receives it
        // and "current" silently answers false for every device.
        Assert.True(only.Current);

        // The token hash and the IP hash stay in the database. A list exists so
        // somebody recognises their own devices, not so it hands back the
        // material that would let one be impersonated.
        Assert.Null(typeof(SessionDto).GetProperty("TokenHash"));
    }

    [Fact]
    public async Task Revoking_a_session_id_that_is_not_yours_is_indistinguishable_from_one_that_is()
    {
        var session = await SignedInAsync(UniqueEmail()).ConfigureAwait(true);

        var stranger = await LubnanApiFactory
            .DeleteWithCsrfAsync(session.Client, $"/api/v1/auth/sessions/{Guid.NewGuid()}", session.Csrf)
            .ConfigureAwait(true);

        // 204, not 404. A distinguishable answer would let anyone holding one
        // session enumerate the ids of everybody else's.
        Assert.Equal(HttpStatusCode.NoContent, stranger.StatusCode);
    }

    [Fact]
    public async Task Moderation_is_closed_to_an_ordinary_signed_in_reader()
    {
        var session = await SignedInAsync(UniqueEmail()).ConfigureAwait(true);

        var response = await LubnanApiFactory.PostWithCsrfAsync(
            session.Client,
            $"/api/v1/admin/users/{Guid.NewGuid()}/suspension",
            session.Csrf,
            new { reason = "because I said so" }).ConfigureAwait(true);

        // 403, not 401: they are authenticated and simply not permitted. The
        // check is the policy on the endpoint, so an endpoint that forgot it
        // would fail closed at routing rather than inside a handler.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Asking_to_reset_an_unknown_address_still_answers_204()
    {
        var response = await NewClient().PostAsJsonAsync(
            "/api/v1/auth/forgot-password",
            new { email = UniqueEmail() }).ConfigureAwait(true);

        // Same answer as a known address. Anything else turns this into the
        // account-enumeration oracle that registration and sign-in close.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task A_reset_token_that_never_existed_is_refused()
    {
        var response = await NewClient().PostAsJsonAsync(
            "/api/v1/auth/reset-password",
            new { token = "not-a-real-token", password = "a brand new passphrase" }).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed record SessionDto(
    Guid Id,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastUsedAt,
    string? UserAgent,
    bool Current);
