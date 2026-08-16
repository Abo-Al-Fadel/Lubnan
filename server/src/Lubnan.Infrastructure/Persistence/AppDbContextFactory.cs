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
    private const string Fallback =
        "Host=localhost;Port=5432;Database=lubnan;Username=lubnan;Password=lubnan";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database") ?? Fallback;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__migrations"))
            .Options;

        return new AppDbContext(options);
    }
}
