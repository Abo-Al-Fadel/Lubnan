using Lubnan.Application.Abstractions;
using Lubnan.Infrastructure.Persistence;
using Lubnan.Infrastructure.Persistence.Interceptors;
using Lubnan.Infrastructure.Persistence.Seed;
using Lubnan.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lubnan.Infrastructure;

/// <summary>
/// Everything this assembly contributes to the container.
/// </summary>
/// <remarks>
/// Called only from <c>Program.cs</c>. Nothing in Application or Domain
/// references this project, which is what makes the dependency arrow point
/// upward: Infrastructure implements interfaces it does not own.
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IClock, SystemClock>();

        // Scoped, because the audit interceptor needs the clock and a future
        // one will need the current user. Registered by type so EF can resolve
        // them from the same scope as the context.
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<DomainEventInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                // Named explicitly rather than left as __EFMigrationsHistory,
                // so the table reads as ours in a schema listing.
                npgsql.MigrationsHistoryTable("__migrations");

                // A transient network blip on a managed database should not
                // become a 500. Six seconds of retry, then give up honestly.
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, TimeSpan.FromSeconds(2), null);
            });

            options.AddInterceptors(
                provider.GetRequiredService<AuditInterceptor>(),
                provider.GetRequiredService<DomainEventInterceptor>());

            // A read path that mutates a tracked entity by accident is a class
            // of bug this removes outright. Slices that write opt back in per
            // query; the ones that read stay fast and safe by default.
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
