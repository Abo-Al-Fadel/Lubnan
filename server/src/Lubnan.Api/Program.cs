using System.Threading.RateLimiting;
using Lubnan.Api.Extensions;
using Lubnan.Api.Middleware;
using Lubnan.Application;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Infrastructure;
using Lubnan.Infrastructure.Persistence;
using Lubnan.Infrastructure.Persistence.Outbox;
using Lubnan.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

// The composition root, and the only file in the solution that knows about
// every layer at once. Everything below is wiring: no behaviour lives here,
// which is why it can stay short as the feature count grows.

var builder = WebApplication.CreateBuilder(args);

var configuredConnection = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Database is not configured. Set it in user secrets, or as ConnectionStrings__Database.");

// Neon, Supabase, Render and Railway all print a postgresql:// URI; Npgsql
// wants ADO.NET keywords. Accepting both means the string a dashboard gave you
// works as pasted, instead of failing at the first query with a message that
// names neither the setting nor the fix.
var connectionString = ConnectionString.Normalise(configuredConnection);

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString, builder.Configuration)
    .AddAuth(builder.Configuration)
    .AddEndpoints(Lubnan.Application.DependencyInjection.Assembly)
    .AddHealth()
    .AddObservability(builder.Configuration);

builder.Services.Configure<OutboxOptions>(
    builder.Configuration.GetSection(OutboxOptions.SectionName));

// Behind a proxy — Next.js rewrites, a load balancer, a platform router — the
// connection address is the proxy's. Without this, every session row records
// one IP and rate limiting partitions everybody into a single bucket.
//
// KnownNetworks and KnownProxies are cleared and then repopulated from
// configuration on purpose: the defaults trust loopback only, and an empty list
// with ForwardLimit means the header is taken from whoever sent it. Trusting an
// unauthenticated header is how an attacker spoofs their address to escape a
// rate limit or to poison an audit log.
builder.Services.Configure<ForwardedHeadersOptions>(forwarded =>
{
    forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    forwarded.ForwardLimit = 1;
    forwarded.KnownNetworks.Clear();
    forwarded.KnownProxies.Clear();

    foreach (var proxy in builder.Configuration.GetSection("KnownProxies").Get<string[]>() ?? [])
    {
        if (System.Net.IPAddress.TryParse(proxy, out var address))
        {
            forwarded.KnownProxies.Add(address);
        }
    }
});

// Every failure, expected or not, leaves as RFC 7807.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();

// ── CORS ────────────────────────────────────────────────────────────────────
//
// The frontend and the API are deployed separately, so every browser call is
// cross-origin. Origins come from configuration and are listed explicitly:
// AllowAnyOrigin cannot be combined with credentials, and the moment sessions
// exist this policy has to carry them.
//
// Serve both halves from one registrable domain — lubnan.app and
// api.lubnan.app — and the pair stays *same-site*, which is what lets the
// refresh cookie be SameSite=Lax. Unrelated domains force SameSite=None, and
// that cookie is third-party: browsers are steadily turning those off.
// This is a deployment decision that decides an authentication design, which
// is why it is written down here rather than discovered later.
const string CorsPolicy = "web";

var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];

builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .WithHeaders(
        "Content-Type",
        "Accept",
        "Accept-Language",
        "Authorization",
        AuthCookies.CsrfHeader,
        CorrelationIdMiddleware.HeaderName)
    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName, "Content-Language")
    .AllowCredentials()
    .SetPreflightMaxAge(TimeSpan.FromHours(1))));

// ── Rate limiting ───────────────────────────────────────────────────────────
//
// Counted in this process's memory. That is correct for one instance and wrong
// for two: with N replicas the effective limit is N times the number below,
// and nothing about it looks wrong in development.
//
// Before the second replica exists this must move to a shared counter in
// Redis. It is recorded here rather than in a document because this is the
// code that has to change.
var limits = builder.Configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>()
             ?? new RateLimitOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimits.Read, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limits.ReadPermitLimit,
            Window = TimeSpan.FromMinutes(1),
        }));

    // Auth is far tighter than write, because the threat is different. A write
    // limit stops one account flooding a feed; this one stops an
    // unauthenticated attacker working through a password list, and stops this
    // API being used as a machine for mailing strangers.
    options.AddPolicy(RateLimits.Auth, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limits.AuthPermitLimit,
            Window = limits.AuthWindow,
        }));

    // Partitioned by user where there is one, and only by IP otherwise, so a
    // single abusive account cannot hide behind a carrier NAT that thousands
    // of legitimate readers share.
    options.AddPolicy(RateLimits.Write, context => RateLimitPartition.GetTokenBucketLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = limits.WriteTokenLimit,
            TokensPerPeriod = limits.WriteTokensPerPeriod,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            AutoReplenishment = true,
        }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        // Tell the client when to come back. A bare 429 invites an immediate
        // retry, which is the behaviour the limit exists to prevent.
        context.HttpContext.Response.Headers.RetryAfter = "60";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                type = "https://lubnan.app/errors/rate-limit",
                title = "Too many requests.",
                status = StatusCodes.Status429TooManyRequests,
                code = "request.rateLimited",
            },
            cancellationToken).ConfigureAwait(false);
    };
});

// ── Output cache ────────────────────────────────────────────────────────────
//
// Places change when an editor publishes, which is rarely. Varying by
// Accept-Language as well as by query string matters: without it the first
// reader's language is served to everyone until the entry expires, and that
// bug only appears once there is a cache in front.
builder.Services.AddOutputCache(options => options.AddPolicy("places", policy => policy
    .Expire(TimeSpan.FromMinutes(5))
    .SetVaryByQuery("region", "category", "locale")
    .SetVaryByHeader("Accept-Language")));

var app = builder.Build();

// Seeding is a command, not a startup step. Two replicas starting together
// would race, and a seeder that runs automatically eventually runs somewhere
// it was not meant to.
//
//   dotnet run --project src/Lubnan.Api -- seed
if (args.Contains("seed", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync().ConfigureAwait(false);
    return;
}

// First, so everything downstream sees the caller's real address and scheme.
app.UseForwardedHeaders();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Lubnān API"));
}
else
{
    // HSTS only outside development: it is remembered by the browser for its
    // full max-age, so setting it on localhost breaks plain HTTP there for
    // every other project on the machine.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicy);
app.UseRateLimiter();

// Order matters and is not arbitrary.
//
// CSRF before authentication: a forged request should be rejected without the
// cost of validating its token, and before anything treats it as a user.
// Authentication before authorization, because a policy cannot check a role
// on a principal that has not been built yet. Output caching last of the four,
// so it never serves one reader's private response to another.
app.UseMiddleware<CsrfMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // No checks at all: this answers "is the process up", and nothing more.
    Predicate = _ => false,
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthExtensions.ReadyTag),
});

await app.RunAsync().ConfigureAwait(false);

/// <summary>Exposed so the integration tests can build a host from it.</summary>
public partial class Program;
