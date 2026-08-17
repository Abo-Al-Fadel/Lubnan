using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lubnan.Application.Abstractions.Http;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

/// <summary>
/// Sign-in, end to end, against a real database.
/// </summary>
/// <remarks>
/// Every test here drives HTTP and asserts on cookies and status codes, because
/// that is the surface an attacker has. A unit test of the handler would pass
/// while the cookie was missing <c>HttpOnly</c>.
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class IdentityEndpointTests(LubnanApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static string UniqueEmail() => $"reader-{Guid.NewGuid():N}@example.com";

    private HttpClient Client => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
        // The cookie handler is the point. Without it every request would look
        // like a new browser and none of the session behaviour would be
        // exercised.
        HandleCookies = true,
    });

    [Fact]
    public async Task Registering_an_address_that_already_exists_is_indistinguishable_from_a_new_one()
    {
        var client = Client;
        var email = UniqueEmail();
        var body = new { email, password = "a long enough passphrase", displayName = "Reader" };

        var first = await client.PostAsJsonAsync("/api/v1/auth/register", body).ConfigureAwait(true);
        var second = await client.PostAsJsonAsync("/api/v1/auth/register", body).ConfigureAwait(true);

        // Identical, on purpose. Any difference — status, body, or timing —
        // turns this endpoint into a way of testing whether an address has an
        // account here.
        Assert.Equal(first.StatusCode, second.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
    }

    [Fact]
    public async Task Registration_refuses_a_short_password()
    {
        // Length is the only password rule. Character classes push people
        // towards Password1! and reject passphrases that are far stronger.
        var response = await Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = UniqueEmail(), password = "short", displayName = "Reader" })
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registration_refuses_something_that_is_not_an_address()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "not-an-email-at-all", password = "a long enough passphrase", displayName = "Reader" })
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registration_refuses_a_display_name_carrying_a_bidi_override()
    {
        // U+202E flips rendering direction for everything after it. On a
        // trilingual site a name containing one can be made to display as a
        // different user's, and escaping does not help because these are
        // legitimate characters in Arabic text.
        var response = await Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = UniqueEmail(), password = "a long enough passphrase", displayName = "Reader‮nimda" })
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_unconfirmed_account_cannot_sign_in()
    {
        var client = Client;
        var email = UniqueEmail();

        await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password = "a long enough passphrase", displayName = "Reader" })
            .ConfigureAwait(true);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "a long enough passphrase" })
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_wrong_password_and_an_unknown_address_answer_identically()
    {
        var client = Client;

        var unknown = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = UniqueEmail(), password = "a long enough passphrase" }).ConfigureAwait(true);

        var wrong = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "byblos@example.com", password = "definitely not the password" }).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

        var a = await unknown.Content.ReadFromJsonAsync<ProblemDto>(Json).ConfigureAwait(true);
        var b = await wrong.Content.ReadFromJsonAsync<ProblemDto>(Json).ConfigureAwait(true);

        // Same code and same wording. "No such account" versus "wrong password"
        // is free reconnaissance for credential stuffing.
        Assert.Equal(a!.Code, b!.Code);
        Assert.Equal(a.Title, b.Title);
    }

    [Fact]
    public async Task Me_is_closed_to_anonymous_callers()
    {
        var response = await Client.GetAsync(new Uri("/api/v1/me", UriKind.Relative)).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(Json).ConfigureAwait(true);
        Assert.Equal("auth.required", problem!.Code);
    }

    [Fact]
    public async Task Refreshing_without_a_cookie_is_a_401_and_not_a_500()
    {
        var response = await Client.PostAsync(new Uri("/api/v1/auth/refresh", UriKind.Relative), null)
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signing_out_always_succeeds_even_with_nothing_to_sign_out_of()
    {
        // A 401 here would leave somebody looking at a page that says they are
        // signed in, on a device they are trying to leave.
        var response = await Client.PostAsync(new Uri("/api/v1/auth/logout", UriKind.Relative), null)
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Session_cookies_are_httpOnly_and_the_csrf_cookie_is_not()
    {
        var client = Client;
        var email = UniqueEmail();

        await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password = "a long enough passphrase", displayName = "Reader" })
            .ConfigureAwait(true);

        await factory.ConfirmAsync(email).ConfigureAwait(true);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "a long enough passphrase" }).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();

        var access = cookies.Single(c => c.StartsWith(AuthCookies.AccessCookie, StringComparison.Ordinal));
        var refresh = cookies.Single(c => c.StartsWith(AuthCookies.RefreshCookie, StringComparison.Ordinal));
        var csrf = cookies.Single(c => c.StartsWith(AuthCookies.CsrfCookie, StringComparison.Ordinal));

        // The two credentials must be unreadable by script, or one compromised
        // frontend dependency walks away with every session.
        Assert.Contains("httponly", access, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", refresh, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", access, StringComparison.OrdinalIgnoreCase);

        // The refresh token travels only to the auth routes, so an ordinary
        // request cannot leak the long-lived credential into a log or a proxy.
        Assert.Contains($"path={AuthCookies.RefreshPath}", refresh, StringComparison.OrdinalIgnoreCase);

        // And the CSRF token must be readable, or the double-submit check has
        // nothing to submit.
        Assert.DoesNotContain("httponly", csrf, StringComparison.OrdinalIgnoreCase);

        // The response body carries no tokens. Returning them would put the
        // credentials somewhere script can reach, defeating the cookies.
        Assert.Equal(0, response.Content.Headers.ContentLength ?? 0);
    }

    [Fact]
    public async Task A_signed_in_reader_can_read_their_own_account()
    {
        var client = Client;
        var email = UniqueEmail();

        await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password = "a long enough passphrase", displayName = "Reader" })
            .ConfigureAwait(true);

        await factory.ConfirmAsync(email).ConfigureAwait(true);

        await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "a long enough passphrase" }).ConfigureAwait(true);

        var me = await client.GetFromJsonAsync<MeDto>("/api/v1/me", Json).ConfigureAwait(true);

        Assert.NotNull(me);
        Assert.Equal(email, me.Email);
        Assert.Equal("Reader", me.DisplayName);
        Assert.Equal("Active", me.State);
        Assert.Equal(1, me.ActiveSessions);
        Assert.False(me.IsAdmin);
    }

    [Fact]
    public async Task Every_response_carries_the_security_headers()
    {
        var response = await Client.GetAsync(new Uri("/api/v1/places", UriKind.Relative)).ConfigureAwait(true);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_csrf_token_of_the_wrong_length_is_forbidden_not_a_server_error()
    {
        var client = Client;
        var email = UniqueEmail();

        await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password = "a long enough passphrase", displayName = "Reader" })
            .ConfigureAwait(true);

        await factory.ConfirmAsync(email).ConfigureAwait(true);

        await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "a long enough passphrase" }).ConfigureAwait(true);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.TryAddWithoutValidation(AuthCookies.CsrfHeader, "short");

        var response = await client.SendAsync(request).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(Json).ConfigureAwait(true);
        Assert.Equal("request.csrf", problem!.Code);
    }

    [Fact]
    public async Task Refreshing_a_signed_out_session_is_not_treated_as_theft()
    {
        var client = Client;
        var email = UniqueEmail();

        await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password = "a long enough passphrase", displayName = "Reader" })
            .ConfigureAwait(true);

        await factory.ConfirmAsync(email).ConfigureAwait(true);

        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "a long enough passphrase" }).ConfigureAwait(true);

        var setCookie = login.Headers.GetValues("Set-Cookie").ToList();
        var refresh = setCookie.Single(c => c.StartsWith(AuthCookies.RefreshCookie, StringComparison.Ordinal));
        var csrf = setCookie.Single(c => c.StartsWith(AuthCookies.CsrfCookie, StringComparison.Ordinal));
        var csrfValue = csrf.Split(';')[0].Split('=')[1];

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logout.Headers.TryAddWithoutValidation(AuthCookies.CsrfHeader, csrfValue);
        await client.SendAsync(logout).ConfigureAwait(true);

        var replay = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.TryAddWithoutValidation(
            "Cookie",
            $"{refresh.Split(';')[0]}; {csrf.Split(';')[0]}");
        refreshRequest.Headers.TryAddWithoutValidation(AuthCookies.CsrfHeader, csrfValue);

        var response = await replay.SendAsync(refreshRequest).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(Json).ConfigureAwait(true);
        Assert.Equal("auth.sessionEnded", problem!.Code);
    }
}

public sealed record MeDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool EmailConfirmed,
    bool IsAdmin,
    string State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PendingDeletionUntil,
    int ActiveSessions);
