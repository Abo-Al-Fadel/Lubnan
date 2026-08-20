using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

/// <summary>
/// Every message the outbox can be handed must actually dispatch.
/// </summary>
/// <remarks>
/// This suite exists because of a specific production failure. The reattempt
/// notice searched <c>outbox_messages.payload</c> with a <c>LIKE</c>; that
/// column is <c>jsonb</c>, PostgreSQL has no <c>jsonb ~~ jsonb</c> operator,
/// and every notice failed with <c>42883</c> and retried until it exhausted
/// MaxAttempts. Nobody registering an address that already existed was ever
/// told.
/// <para>
/// The whole suite stayed green throughout, because the outbox was switched off
/// in the test host — so the consumers were never once executed. A handler that
/// no test ever runs is a handler that is only tested by the people using it.
/// </para>
/// <para>
/// These assert on the <c>error</c> column rather than on a mailbox. What broke
/// was the database query inside the consumer, not the mail, and the error
/// column is where a translation failure lands.
/// </para>
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class OutboxDispatchTests(LubnanApiFactory factory)
{
    private const string Password = "a long enough passphrase";

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task Registering_dispatches_its_confirmation_without_error()
    {
        var client = NewClient();

        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"outbox-{Guid.NewGuid():N}@example.com", password = Password, displayName = "Outbox QA" })
            .ConfigureAwait(true);

        var failures = await factory.DrainOutboxAsync().ConfigureAwait(true);

        Assert.True(failures.Count == 0, $"outbox reported: {string.Join(" | ", failures)}");
    }

    [Fact]
    public async Task Registering_an_address_twice_dispatches_the_reattempt_notice()
    {
        // The exact shape that failed in production: the second registration
        // takes the reattempt branch, which is the one that queried jsonb.
        var client = NewClient();
        var email = $"outbox-twice-{Guid.NewGuid():N}@example.com";
        var body = new { email, password = Password, displayName = "Outbox QA" };

        await client.PostAsJsonAsync("/api/v1/auth/register", body).ConfigureAwait(true);
        await factory.DrainOutboxAsync().ConfigureAwait(true);

        await client.PostAsJsonAsync("/api/v1/auth/register", body).ConfigureAwait(true);

        var failures = await factory.DrainOutboxAsync().ConfigureAwait(true);

        // Before the fix this said:
        //   42883: operator does not exist: jsonb ~~ jsonb
        Assert.True(failures.Count == 0, $"outbox reported: {string.Join(" | ", failures)}");
    }

    [Fact]
    public async Task Asking_for_a_password_reset_leaves_the_outbox_clean()
    {
        var client = NewClient();
        var email = $"outbox-reset-{Guid.NewGuid():N}@example.com";

        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = Password, displayName = "Outbox QA" }).ConfigureAwait(true);
        await factory.ConfirmAsync(email).ConfigureAwait(true);

        await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email }).ConfigureAwait(true);

        var failures = await factory.DrainOutboxAsync().ConfigureAwait(true);

        Assert.True(failures.Count == 0, $"outbox reported: {string.Join(" | ", failures)}");
    }
}
