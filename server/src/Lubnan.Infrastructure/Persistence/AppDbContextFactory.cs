using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lubnan.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build the model without starting the API.
/// </summary>
/// <remarks>
/// Without this, the tooling boots <c>Program.cs</c> to find a context — which
/// means adding a migration needs the API's whole configuration to be valid,
/// including secrets that a developer writing a migration has no reason to
/// hold.
/// <para>
/// The connection string here is only used to pick a provider and read its
/// version conventions. Generating a migration never connects.
/// </para>
/// </remarks>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    // Port 5433: the container publishes there so it cannot collide with a
    // natively installed PostgreSQL, which owns 5432 on most machines that
    // have ever run the EDB installer. See docker-compose.yml.
    private const string Fallback =
        "Host=localhost;Port=5433;Database=lubnan;Username=lubnan;Password=lubnan";

    public AppDbContext CreateDbContext(string[] args)
    {
        // Normalised, because this is the path `dotnet ef database update` takes
        // and the string in that environment variable is usually the URI a
        // managed provider printed. Migrating against Neon is the first thing
        // anybody does with a real connection string, so it is the first place
        // the format mismatch would bite.
        var connectionString = ConnectionString.Normalise(
            Environment.GetEnvironmentVariable("ConnectionStrings__Database") ?? Fallback);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__migrations"))
            .Options;

        return new AppDbContext(options);
    }
}
