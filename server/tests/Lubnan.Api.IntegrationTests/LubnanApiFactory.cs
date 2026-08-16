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
