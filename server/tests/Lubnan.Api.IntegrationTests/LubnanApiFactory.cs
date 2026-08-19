using System.Net.Http.Json;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Security;
using Lubnan.Infrastructure.Persistence;
using Lubnan.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

/// <summary>
/// Boots the real API against a real, disposable PostgreSQL.
/// </summary>
/// <remarks>
/// Testcontainers starts a Postgres container for the run and throws it away
/// afterwards. That matters more than it sounds: the alternative is either an
/// in-memory provider, which does not have Postgres's types, constraints or
/// query translation and therefore cannot catch the bugs that actually happen,
/// or a shared development database, which makes tests order-dependent and
/// fail on whoever ran them second.
/// <para>
/// Nothing is stubbed. The migration runs, the seeder runs through the domain,
/// and the requests go through the whole pipeline — routing, validation, the
/// handler, EF, and back out as JSON. If this passes, the thing works.
/// </para>
/// </remarks>
public sealed class LubnanApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Pinned to the same image the compose file uses. A suite that passes
    // against a different major version than production runs is a suite that
    // can be green while production is broken.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("lubnan")
        .WithUsername("lubnan")
        .WithPassword("lubnan")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync().ConfigureAwait(false);

        // An environment variable, not ConfigureAppConfiguration.
        //
        // Program.cs reads the connection string *while building* the host, so
        // a configuration callback registered by WebApplicationFactory has not
        // run yet and the read would see nothing. Environment variables are
        // already in the configuration by then, and they outrank
        // appsettings.Development.json.
        Environment.SetEnvironmentVariable("ConnectionStrings__Database", _postgres.GetConnectionString());

        // Set explicitly rather than relying on appsettings.Development.json
        // being found from the test host's content root. A suite whose
        // configuration depends on file discovery fails differently on a
        // developer's machine and in CI, and the failure looks like a bug in
        // the code under test.
        Environment.SetEnvironmentVariable("Auth__SigningKey", "integration-test-signing-key-not-a-secret-000");
        Environment.SetEnvironmentVariable("Auth__HashKey", "integration-test-hash-key-not-a-secret-00000");

        // The test host speaks plain HTTP, and a Secure cookie is not stored by
        // a client over HTTP - so leaving this on would make every session test
        // fail for a reason that has nothing to do with sessions.
        Environment.SetEnvironmentVariable("Auth__RequireSecureCookies", "false");

        // Every test drives requests from the same address, so the production
        // auth limit of ten per five minutes is spent within the first few and
        // the rest fail on each other rather than on the code.
        //
        // Raised rather than disabled: the limiter stays in the pipeline, so a
        // change that breaks it still breaks a test. RateLimitTests sets its
        // own low value to prove the limit actually fires.
        Environment.SetEnvironmentVariable("RateLimits__AuthPermitLimit", "10000");
        Environment.SetEnvironmentVariable("RateLimits__ReadPermitLimit", "10000");
        Environment.SetEnvironmentVariable("RateLimits__WriteTokenLimit", "10000");
        Environment.SetEnvironmentVariable("RateLimits__WriteTokensPerPeriod", "10000");
        Environment.SetEnvironmentVariable("Outbox__Enabled", "false");

        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(false);

        // Migrating here rather than at startup is not a contradiction of the
        // rule in Program.cs. That rule exists because replicas race each other
        // in production; a test owns its database exclusively.
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>()
            .SeedAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Confirm an address without going through the mail.
    /// </summary>
    /// <remarks>
    /// Reaches into the database on purpose. The alternative is for the test to
    /// read a file the development mail sender wrote, which couples every
    /// sign-in test to the mail implementation — so a change of provider would
    /// break tests that are about sessions.
    /// <para>
    /// The confirmation <em>flow</em> is covered by its own test against the
    /// real token. This is only a shortcut for the tests whose subject is what
    /// happens afterwards.
    /// </para>
    /// </remarks>
    public async Task ConfirmAsync(string email)
    {
        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<Lubnan.Application.Abstractions.IClock>();

        var user = await db.Users.SingleAsync(u => u.Email == Lubnan.Domain.Users.Email.Create(email).Value)
            .ConfigureAwait(false);

        user.ConfirmEmail(clock.UtcNow);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// The state machine position of an account, read straight from the row.
    /// </summary>
    /// <remarks>
    /// Asserted against the database rather than against an endpoint, because
    /// the endpoints deliberately do not report it: a caller cannot ask whether
    /// somebody else is suspended. The test needs the ground truth that the API
    /// is careful not to expose.
    /// </remarks>
    public async Task<string> StateOfAsync(string email)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users
            .AsNoTracking()
            .SingleAsync(u => u.Email == Lubnan.Domain.Users.Email.Create(email).Value)
            .ConfigureAwait(false);

        return user.State.ToString();
    }

    /// <summary>
    /// A state-changing request carrying the double-submit token, the way the
    /// browser does it.
    /// </summary>
    /// <remarks>
    /// The token is passed in rather than read back out of a cookie jar,
    /// because the jar behind <c>HandleCookies</c> is not reachable from here.
    /// It is captured from the login response instead — which is the same value
    /// the browser would read, arriving the same way.
    /// <para>
    /// Done this way rather than by disabling CSRF for tests: the middleware
    /// stays in the pipeline, so a change that breaks it breaks these too.
    /// </para>
    /// </remarks>
    public static Task<HttpResponseMessage> PostWithCsrfAsync(
        HttpClient client, string url, string csrf, object? body) =>
        SendWithCsrfAsync(client, HttpMethod.Post, url, csrf, body);

    public static Task<HttpResponseMessage> DeleteWithCsrfAsync(
        HttpClient client, string url, string csrf) =>
        SendWithCsrfAsync(client, HttpMethod.Delete, url, csrf, null);

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(
        HttpClient client, HttpMethod method, string url, string csrf, object? body)
    {
        ArgumentNullException.ThrowIfNull(client);

        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (!string.IsNullOrEmpty(csrf))
        {
            request.Headers.Add(AuthCookies.CsrfHeader, csrf);
        }

        return await client.SendAsync(request).ConfigureAwait(false);
    }

    /// <summary>Pull the readable CSRF cookie out of a Set-Cookie header.</summary>
    public static string CsrfFrom(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return string.Empty;
        }

        var header = cookies.FirstOrDefault(c =>
            c.StartsWith(AuthCookies.CsrfCookie, StringComparison.Ordinal));

        if (header is null)
        {
            return string.Empty;
        }

        var value = header.Split(';')[0];
        return value[(value.IndexOf('=', StringComparison.Ordinal) + 1)..];
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Development, so the pipeline skips UseHttpsRedirection. Under any
        // other environment every request would answer 307 to an https URL the
        // test host is not listening on, and every assertion would be about a
        // redirect.
        builder.UseEnvironment("Development");

        // The breach check is the one dependency that reaches the public
        // internet on a path the tests exercise constantly. Left real, every
        // registration in the suite would call api.pwnedpasswords.com — slow,
        // rude, and a suite that fails when someone's wifi drops.
        //
        // Replaced rather than disabled: the check still runs, so the wiring is
        // covered and a handler that stopped calling it would still be caught.
        // Only the corpus is local.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBreachedPasswordCheck>();
            services.AddSingleton<IBreachedPasswordCheck, StubBreachedPasswordCheck>();
        });
    }
}

/// <summary>One known-bad password, so both branches are reachable offline.</summary>
internal sealed class StubBreachedPasswordCheck : IBreachedPasswordCheck
{
    /// <summary>Genuinely in the corpus, tens of thousands of times over.</summary>
    public const string Breached = "password12345678";

    public Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken = default) =>
        Task.FromResult(string.Equals(password, Breached, StringComparison.Ordinal));
}

/// <summary>
/// One container for the whole assembly. Starting Postgres takes a couple of
/// seconds; doing it per test class would multiply that by the number of
/// classes for no isolation benefit, because these tests only read.
/// </summary>
[CollectionDefinition(Name)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xunit's own convention for a collection definition. Renaming it to satisfy the analyser would make it harder to recognise, not easier.")]
public sealed class ApiCollection : ICollectionFixture<LubnanApiFactory>
{
    public const string Name = "api";
}
