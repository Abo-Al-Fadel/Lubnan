using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

/// <summary>
/// A password that is already on a list cannot be used, at either of the two
/// places one gets chosen.
/// </summary>
/// <remarks>
/// This is the third of the three things NIST SP 800-63B and the NCSC actually
/// say. Length and "allow every character" are in the validator and easy to
/// verify by reading it; the breach check is the half that only means anything
/// if it is wired to the endpoints, which is what these assert.
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class BreachedPasswordTests(LubnanApiFactory factory)
{
    private static string UniqueEmail() => $"breach-{Guid.NewGuid():N}@example.com";

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task Registering_with_a_breached_password_is_refused()
    {
        var response = await NewClient().PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                email = UniqueEmail(),
                password = StubBreachedPasswordCheck.Breached,
                displayName = "Breach QA",
            }).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDto>(new System.Text.Json.JsonSerializerOptions(
                System.Text.Json.JsonSerializerDefaults.Web))
            .ConfigureAwait(true);

        Assert.Equal("password.breached", problem!.Code);
    }

    [Fact]
    public async Task A_password_that_is_long_enough_and_unbreached_is_accepted()
    {
        // The counterpart to the test above: it is easy to write a check that
        // rejects everything and looks like it works.
        var response = await NewClient().PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                email = UniqueEmail(),
                password = "a long enough passphrase",
                displayName = "Breach QA",
            }).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Resetting_to_a_breached_password_is_refused_before_the_token_is_spent()
    {
        // The token here is invalid, so ordinarily this answers 400
        // token.invalid. It answers password.breached instead, which proves the
        // check runs *first* — the ordering that matters, because rejecting the
        // password after consuming the token would strand somebody mid-reset
        // holding a link that no longer works.
        var response = await NewClient().PostAsJsonAsync(
            "/api/v1/auth/reset-password",
            new { token = "not-a-real-token", password = StubBreachedPasswordCheck.Breached })
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDto>(new System.Text.Json.JsonSerializerOptions(
                System.Text.Json.JsonSerializerDefaults.Web))
            .ConfigureAwait(true);

        Assert.Equal("password.breached", problem!.Code);
    }
}
