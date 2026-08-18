using System.Security.Claims;
using System.Text;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Security;
using Lubnan.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Lubnan.Api.Extensions;

/// <summary>Authentication and authorization, wired once.</summary>
public static class AuthExtensions
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Validated on startup. A deployment missing its signing key should
        // fail to boot, not fail on the first sign-in after it has been rolled
        // out everywhere and marked healthy.
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearer =>
            {
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ValidateLifetime = true,

                    // The default is five minutes, which quietly makes a
                    // fifteen-minute token last twenty. Sessions are revoked by
                    // expiry here, so that slack is the revocation window and
                    // it should be as small as clocks allow.
                    ClockSkew = TimeSpan.FromSeconds(30),

                    // Only the algorithm we issue. Without this, a token
                    // presented with a different alg header is handed to a
                    // different validator, which is the family of bugs that
                    // starts with "alg: none".
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role,
                };

                bearer.Events = new JwtBearerEvents
                {
                    // The token arrives in an httpOnly cookie, not in an
                    // Authorization header, because a header has to be set by
                    // script and a value script can set is a value script can
                    // read - and therefore a value an XSS payload can steal.
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies[AuthCookies.AccessCookie];
                        return Task.CompletedTask;
                    },

                    // Never tell the caller why. "Token expired" versus
                    // "signature invalid" is free reconnaissance, and the
                    // client's correct behaviour is identical either way:
                    // refresh, then sign in.
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/problem+json";

                        return context.Response.WriteAsync(
                            """
                            {"type":"https://lubnan.app/errors/unauthorized",
                             "title":"Sign in to continue.",
                             "status":401,
                             "code":"auth.required"}
                            """);
                    },
                };
            });

        services.AddAuthorization(auth =>
        {
            auth.AddPolicy(Policies.CanModerate, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(Roles.Admin));

            // Authenticated by default is not set here on purpose: most of this
            // API is a public catalogue, and a fallback policy would mean every
            // new read endpoint is private until somebody remembers otherwise.
            // Endpoints opt in with RequireAuthorization.
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}

/// <summary>Who is asking, read from the validated token.</summary>
internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? Id =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
