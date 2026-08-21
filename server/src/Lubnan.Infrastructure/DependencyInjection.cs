using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Security;
using Lubnan.Application.Features.Identity;
using Lubnan.Infrastructure.Flights;
using Lubnan.Infrastructure.Jobs;
using Lubnan.Infrastructure.Mail;
using Lubnan.Infrastructure.Persistence;
using Lubnan.Infrastructure.Persistence.Interceptors;
using Lubnan.Infrastructure.Persistence.Outbox;
using Lubnan.Infrastructure.Persistence.Seed;
using Lubnan.Infrastructure.Security;
using Lubnan.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        string connectionString,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

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
        services.AddSingleton<IImageSanitiser, ImageSanitiser>();

        // Mail: the provider is a configuration value, not a code change.
        //
        // Development writes to disk so a fresh clone runs the whole
        // registration flow with no account anywhere. Production posts to
        // Resend. Both sit behind IEmailSender and no handler knows which.
        services.AddOptions<MailOptions>()
            .Bind(configuration.GetSection(MailOptions.SectionName));

        var mail = configuration.GetSection(MailOptions.SectionName).Get<MailOptions>() ?? new MailOptions();

        if (string.Equals(mail.Provider, "resend", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
            {
                client.BaseAddress = new Uri("https://api.resend.com/");
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mail.ApiKey);
            });
        }
        else
        {
            services.AddSingleton<IEmailSender, FileEmailSender>();
        }

        // Breached-password checking, over k-anonymity so the password never
        // leaves this process. Free, keyless, and short-timeout because a
        // registration must not wait on a third party.
        services.AddHttpClient<IBreachedPasswordCheck, HibpBreachedPasswordCheck>(client =>
        {
            client.BaseAddress = new Uri("https://api.pwnedpasswords.com/");
            client.Timeout = TimeSpan.FromSeconds(3);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Lubnan/1.0 (+https://github.com/Abo-Al-Fadel/Lubnan)");
        });

        // Anonymises accounts past their grace period. The only thing in the
        // system that can reach AccountState.Anonymised.
        services.AddOptions<PurgeOptions>()
            .Bind(configuration.GetSection(PurgeOptions.SectionName));
        services.AddHostedService<AccountPurgeWorker>();

        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName));
        services.AddHostedService<OutboxProcessor>();

        services.AddOptions<FlightOptions>()
            .Bind(configuration.GetSection(FlightOptions.SectionName));

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
