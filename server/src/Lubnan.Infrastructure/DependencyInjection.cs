using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Security;
using Lubnan.Application.Features.Identity;
using Lubnan.Infrastructure.Flights;
using Lubnan.Infrastructure.Mail;
using Lubnan.Infrastructure.Persistence;
using Lubnan.Infrastructure.Persistence.Interceptors;
using Lubnan.Infrastructure.Persistence.Outbox;
using Lubnan.Infrastructure.Persistence.Seed;
using Lubnan.Infrastructure.Security;
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

            // Tracking stays on by default, and the reason is asymmetry of
            // failure.
            //
            // This was NoTracking for one commit, on the reasoning that most
            // queries are reads and a read that accidentally mutates is a bug.
            // The integration tests caught what that actually costs: a handler
            // that loads a user, changes it and calls SaveChanges writes
            // *nothing*, because the entity it changed was never tracked. No
            // error, no warning, a 200 response, and the change gone.
            //
            // Forgetting AsNoTracking on a read costs some memory. Forgetting
            // AsTracking on a write costs the write. The safe default is the
            // one whose failure is a slow query rather than silent data loss,
            // and read handlers already ask for AsNoTracking explicitly.
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<DatabaseSeeder>();

        // Singletons: all four are stateless, and PasswordHasher in particular
        // computes its decoy hash once at construction. Making it scoped would
        // pay for that on every request.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenFactory, TokenFactory>();
        services.AddSingleton<IIpHasher, IpHasher>();
        services.AddSingleton<IEmailTombstoner, EmailTombstoner>();

        // Development writes mail to disk. A provider goes here behind the same
        // interface, and no handler changes.
        services.AddSingleton<IEmailSender, FileEmailSender>();

        services.AddOptions<OutboxOptions>();
        services.AddHostedService<OutboxProcessor>();

        services.AddHttpClient<IFlightBoard, BeirutAirportFlightBoard>(client =>
        {
            client.BaseAddress = new Uri("https://www.beirutairport.gov.lb/");
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Lubnan/1.0 (+https://github.com/Abo-Al-Fadel/Lubnan)");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        });

        return services;
    }
}
