using Lubnan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lubnan.Api.Extensions;

/// <summary>
/// Two probes, answering two different questions.
/// </summary>
/// <remarks>
/// <c>/health/live</c> asks whether the process is running. It touches nothing
/// external, because a liveness probe that fails when the database is briefly
/// unreachable makes the orchestrator restart every replica during a database
/// failover — turning a thirty-second blip into an outage.
/// <para>
/// <c>/health/ready</c> asks whether this instance can serve traffic, which
/// does depend on the database. A failure here removes one instance from the
/// load balancer and leaves it running.
/// </para>
/// <para>
/// Conflating the two is the most common way a Kubernetes deployment turns a
/// recoverable incident into a restart loop.
/// </para>
/// </remarks>
public static class HealthExtensions
{
    public const string ReadyTag = "ready";

    public static IServiceCollection AddHealth(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: [ReadyTag]);

        return services;
    }
}

/// <summary>
/// Written here rather than taken from a package: it is six lines, and it uses
/// the context the application already configures, so it exercises the same
/// connection string, pool and retry policy the real queries do.
/// </summary>
internal sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The database refused the connection.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The message is for an operator reading a probe endpoint on an
            // internal network, not for a user.
            return HealthCheckResult.Unhealthy("The database is unreachable.", exception);
        }
    }
}
