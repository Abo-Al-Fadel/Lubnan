using Lubnan.Infrastructure.Persistence;
using Lubnan.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    }
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
